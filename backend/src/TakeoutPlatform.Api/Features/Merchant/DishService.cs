using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Menu;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class DishService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public DishService(AppDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<(bool MerchantFound, DishResponse? Dish)> CreateAsync(
        CreateDishRequest request,
        CancellationToken cancellationToken)
    {
        var merchantExists = await _db.Merchants
            .AnyAsync(merchant => merchant.Id == request.MerchantId, cancellationToken);

        if (!merchantExists)
        {
            return (false, null);
        }

        var dish = new Dish
        {
            MerchantId = request.MerchantId,
            Name = request.Name!.Trim(),
            Price = request.Price,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Inventory = request.Inventory,
        };

        ApplyMenuItem(dish, MenuItemValidation.Create(request));
        _db.Dishes.Add(dish);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, ToResponse(dish));
    }

    public async Task<IReadOnlyList<DishResponse>?> ListAsync(
        int merchantId,
        CancellationToken cancellationToken)
    {
        var merchantExists = await _db.Merchants
            .AnyAsync(merchant => merchant.Id == merchantId, cancellationToken);

        if (!merchantExists)
        {
            return null;
        }

        var dishes = await _db.Dishes
            .AsNoTracking()
            .Where(dish => dish.MerchantId == merchantId)
            .OrderBy(dish => dish.Id)
            .ToListAsync(cancellationToken);
        return dishes.Select(ToResponse).ToList();
    }

    public async Task<DishResponse?> UpdateAsync(
        int dishId,
        int merchantId,
        UpdateDishRequest request,
        CancellationToken cancellationToken)
    {
        var dish = await _db.Dishes
            .SingleOrDefaultAsync(
                item => item.Id == dishId && item.MerchantId == merchantId,
                cancellationToken);

        if (dish is null)
        {
            return null;
        }

        dish.Name = request.Name!.Trim();
        dish.Price = request.Price;
        dish.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        dish.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        dish.Inventory = request.Inventory;
        ApplyMenuItem(dish, MenuItemValidation.Create(request));

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(dish);
    }

    public async Task<bool> DeleteAsync(
        int dishId,
        int merchantId,
        CancellationToken cancellationToken)
    {
        var dish = await _db.Dishes
            .SingleOrDefaultAsync(
                item => item.Id == dishId && item.MerchantId == merchantId,
                cancellationToken);

        if (dish is null)
        {
            return false;
        }

        if (await _db.InventoryReservations.AnyAsync(
            reservation => reservation.DishId == dishId, cancellationToken))
            return false;
        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ApplyMenuItem(Dish dish, MenuItem item)
    {
        dish.ItemType = item.Type;
        dish.BundleSize = item.Options.BundleSize;
        dish.PromotionStartsAtUtc = item.Options.PromotionStartsAtUtc;
        dish.PromotionEndsAtUtc = item.Options.PromotionEndsAtUtc;
    }

    private DishResponse ToResponse(Dish dish) => new(
        dish.Id,
        dish.MerchantId,
        dish.Name,
        dish.Price,
        dish.Category,
        dish.ImageUrl,
        dish.Inventory,
        dish.ItemType.ToString(),
        dish.BundleSize,
        dish.PromotionStartsAtUtc,
        dish.PromotionEndsAtUtc,
        dish.Inventory > 0 && MenuItemFactory.FromDish(dish).IsAvailableAt(_timeProvider.GetUtcNow()));
}
