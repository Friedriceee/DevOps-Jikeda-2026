using Microsoft.EntityFrameworkCore;
using Npgsql;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Menu;

namespace TakeoutPlatform.Api.Features.Inventory;

public enum InventoryOutcome
{
    Success, AlreadyReserved, AlreadyReleased, InvalidRequest, OrderNotFound,
    DishNotFound, InsufficientStock, Unavailable, ReservationConflict, ReservationNotFound,
}

public sealed record InventoryReservationItem(int MerchantId, int DishId, int Quantity);
public sealed record InventoryResult(InventoryOutcome Outcome, string? Message = null)
{
    public bool Succeeded => Outcome is InventoryOutcome.Success
        or InventoryOutcome.AlreadyReserved or InventoryOutcome.AlreadyReleased;
}

public sealed class InventoryService(AppDbContext db, TimeProvider timeProvider)
{
    public async Task<InventoryResult> ReserveAsync(int orderId,
        IReadOnlyList<InventoryReservationItem> items, CancellationToken cancellationToken)
    {
        if (orderId <= 0 || items.Count == 0 || items.Any(item =>
            item.MerchantId <= 0 || item.DishId <= 0 || item.Quantity <= 0))
            return new(InventoryOutcome.InvalidRequest, "An order and positive item quantities are required.");
        InventoryReservationItem[] normalized;
        try
        {
            normalized = items.GroupBy(item => new { item.MerchantId, item.DishId })
                .Select(group => new InventoryReservationItem(group.Key.MerchantId, group.Key.DishId,
                    checked((int)group.Sum(item => (long)item.Quantity))))
                .OrderBy(item => item.DishId).ToArray();
        }
        catch (OverflowException)
        {
            return new(InventoryOutcome.InvalidRequest, "The requested quantity is too large.");
        }

        try
        {
            var result = await InTransactionAsync(async () =>
            {
                if (!await db.Orders.AnyAsync(order => order.Id == orderId, cancellationToken))
                    return new(InventoryOutcome.OrderNotFound, "Order was not found.");
                var existing = await ExistingAsync(orderId, cancellationToken);
                if (existing.Count > 0) return CheckReplay(existing, normalized);
                var now = timeProvider.GetUtcNow();
                foreach (var item in normalized)
                {
                    var dish = await db.Dishes.AsNoTracking().SingleOrDefaultAsync(
                        dish => dish.Id == item.DishId && dish.MerchantId == item.MerchantId,
                        cancellationToken);
                    if (dish is null) return new(InventoryOutcome.DishNotFound, "Menu item was not found.");
                    if (!MenuItemFactory.FromDish(dish).IsAvailableAt(now))
                        return new(InventoryOutcome.Unavailable, "This promotional item is not currently available.");

                    // The stock condition and subtraction execute in one SQL UPDATE.
                    // PostgreSQL rechecks this predicate after competing row locks.
                    var changed = await db.Dishes.Where(dish => dish.Id == item.DishId
                        && dish.MerchantId == item.MerchantId && dish.Inventory >= item.Quantity)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(
                            dish => dish.Inventory, dish => dish.Inventory - item.Quantity), cancellationToken);
                    if (changed != 1)
                        return new(InventoryOutcome.InsufficientStock, "There is not enough stock for this item.");

                    db.InventoryReservations.Add(new InventoryReservation
                    {
                        OrderId = orderId, MerchantId = item.MerchantId, DishId = item.DishId,
                        Quantity = item.Quantity, ReservedAtUtc = now,
                    });
                }
                await db.SaveChangesAsync(cancellationToken);
                return new(InventoryOutcome.Success);
            }, cancellationToken);
            // A replay may have waited for the first reservation's row lock and
            // then failed the stock predicate. Check its committed ledger only
            // after our partial stock changes have been rolled back.
            if (result.Outcome == InventoryOutcome.InsufficientStock)
            {
                var concurrent = await ExistingAsync(orderId, cancellationToken);
                if (concurrent.Count > 0) return CheckReplay(concurrent, normalized);
            }
            return result;
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent replay won the unique (OrderId, DishId) key. Our
            // transaction/savepoint has already rolled its stock changes back.
            return CheckReplay(await ExistingAsync(orderId, cancellationToken), normalized);
        }
    }

    public Task<InventoryResult> ReleaseAsync(int orderId, int? merchantId,
        CancellationToken cancellationToken) => InTransactionAsync(async () =>
    {
        var rows = await db.InventoryReservations.AsNoTracking()
            .Where(row => row.OrderId == orderId && (!merchantId.HasValue || row.MerchantId == merchantId))
            .OrderBy(row => row.DishId).ToListAsync(cancellationToken);
        if (rows.Count == 0) return new(InventoryOutcome.ReservationNotFound, "No inventory reservation was found.");
        var restored = 0;
        var now = timeProvider.GetUtcNow();
        foreach (var row in rows.Where(row => !row.IsReleased))
        {
            var claimed = await db.InventoryReservations.Where(item => item.OrderId == orderId
                && item.DishId == row.DishId && !item.IsReleased)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsReleased, true)
                    .SetProperty(item => item.ReleasedAtUtc, now), cancellationToken);
            if (claimed == 0) continue;
            var changed = await db.Dishes.Where(dish => dish.Id == row.DishId
                && dish.MerchantId == row.MerchantId && dish.Inventory <= int.MaxValue - row.Quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    dish => dish.Inventory, dish => dish.Inventory + row.Quantity), cancellationToken);
            if (changed != 1) return new(InventoryOutcome.DishNotFound, "The reserved item could not be restored.");
            restored++;
        }
        return new(restored == 0 ? InventoryOutcome.AlreadyReleased : InventoryOutcome.Success);
    }, cancellationToken);

    private Task<List<InventoryReservation>> ExistingAsync(int orderId, CancellationToken cancellationToken) =>
        db.InventoryReservations.AsNoTracking().Where(row => row.OrderId == orderId)
            .OrderBy(row => row.DishId).ToListAsync(cancellationToken);

    private static InventoryResult CheckReplay(IReadOnlyList<InventoryReservation> existing,
        IReadOnlyList<InventoryReservationItem> requested) => existing.Count == requested.Count
        && existing.Zip(requested).All(pair => !pair.First.IsReleased
            && pair.First.MerchantId == pair.Second.MerchantId && pair.First.DishId == pair.Second.DishId
            && pair.First.Quantity == pair.Second.Quantity)
        ? new(InventoryOutcome.AlreadyReserved)
        : new(InventoryOutcome.ReservationConflict, "This order has a different or released reservation.");

    private async Task<InventoryResult> InTransactionAsync(Func<Task<InventoryResult>> operation,
        CancellationToken cancellationToken)
    {
        var outer = db.Database.CurrentTransaction;
        await using var owned = outer is null
            ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var transaction = outer ?? owned!;
        var savepoint = "inventory_" + Guid.NewGuid().ToString("N");
        if (outer is not null) await transaction.CreateSavepointAsync(savepoint, cancellationToken);
        try
        {
            var result = await operation();
            if (result.Succeeded)
            {
                if (owned is not null) await owned.CommitAsync(cancellationToken);
                else await transaction.ReleaseSavepointAsync(savepoint, cancellationToken);
            }
            else await RollbackAsync();
            return result;
        }
        catch
        {
            await RollbackAsync();
            throw;
        }

        async Task RollbackAsync()
        {
            if (owned is not null) await owned.RollbackAsync(CancellationToken.None);
            else await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None);
            foreach (var entry in db.ChangeTracker.Entries<InventoryReservation>()
                .Where(entry => entry.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;
        }
    }
}
