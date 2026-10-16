using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Inventory;

namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// 创建订单的结果类型，用于向 Controller 区分不同失败原因。
/// </summary>
public enum CreateOrderOutcome
{
    Success,
    EmptyCart,          // 购物车为空
    DishNotFound,       // 某个菜品不存在
    InsufficientStock,  // 某个菜品库存不足
    InvalidInventoryRequest,
    Unavailable,
}

/// <summary>
/// 取消订单的结果类型。
/// </summary>
public enum CancelOrderOutcome
{
    Success,
    NotFound,       // 订单不存在
    NotCancellable, // 订单状态不允许取消（仅待支付可取消）
}

public sealed record CreateOrderResult(
    CreateOrderOutcome Outcome,
    OrderResponse? Order,
    string? Message);

public sealed record CancelOrderResult(
    CancelOrderOutcome Outcome,
    string? Message);

/// <summary>
/// 订单服务。对应旧项目 UserController 的 CreateOrder / getOrders / deleteOrder。
///
/// 关键改动（相对旧项目）：
/// - 订单状态用 OrderStatus 枚举，创建即 Pending。
/// - 取消加了状态守卫：仅 Pending（待支付）订单可取消，与旧项目"任何订单都能删"不同。
/// - 主键自增，去掉旧项目的手动发号。
/// 其余组装逻辑（五表、库存校验与扣减、事务）严格参照旧 CreateOrder。
/// </summary>
public sealed class OrderService
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventory;

    public OrderService(AppDbContext db, InventoryService? inventory = null)
    {
        _db = db;
        _inventory = inventory ?? new InventoryService(db, TimeProvider.System);
    }

    /// <summary>
    /// 创建待支付订单（Order Builder）。整个过程在一个事务里完成：
    /// 组装 Order 主表 → 可选 OrderCoupon → OrderUser → OrderRider →
    /// 逐项校验菜品与库存并扣减、生成 OrderDish。任一步失败则整体回滚。
    /// </summary>
    public async Task<CreateOrderResult> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        // 参照旧项目：购物车为空直接拒绝。
        if (request.ShoppingCart is null || request.ShoppingCart.Length == 0)
        {
            return new CreateOrderResult(CreateOrderOutcome.EmptyCart, null, "购物车不能为空");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = new Order
            {
                Price = request.Price,
                OrderTimestamp = request.OrderTimestamp == default
                    ? DateTime.UtcNow
                    : AsUtc(request.OrderTimestamp),
                ExpectedTimeOfArrival = null,
                RealTimeOfArrival = null,
                Status = OrderStatus.Pending,   // 旧项目 State = 0
                NeedUtensils = request.NeedUtensils,
                AddressId = request.AddressId,
                MerchantRating = null,
                RiderRating = null,
                Comment = null,
            };

            _db.Orders.Add(order);
            // 先保存主表以拿到自增 Id（旧项目靠手动发号，这里靠数据库）。
            await _db.SaveChangesAsync(cancellationToken);

            if (request.CouponId > 0)
            {
                _db.OrderCoupons.Add(new OrderCoupon
                {
                    OrderId = order.Id,
                    UserId = request.UserId,
                    CouponId = request.CouponId,
                    ExpirationDate = AsUtc(request.ExpirationDate),
                });
            }

            _db.OrderUsers.Add(new OrderUser
            {
                OrderId = order.Id,
                UserId = request.UserId,
            });

            _db.OrderRiders.Add(new OrderRider
            {
                OrderId = order.Id,
                RiderPrice = request.RiderPrice,
            });

            var reservation = await _inventory.ReserveAsync(order.Id, request.ShoppingCart
                .Select(item => new InventoryReservationItem(item.MerchantId, item.DishId, item.DishNum))
                .ToArray(), cancellationToken);
            if (!reservation.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                var outcome = reservation.Outcome switch
                {
                    InventoryOutcome.DishNotFound => CreateOrderOutcome.DishNotFound,
                    InventoryOutcome.InsufficientStock => CreateOrderOutcome.InsufficientStock,
                    InventoryOutcome.Unavailable => CreateOrderOutcome.Unavailable,
                    _ => CreateOrderOutcome.InvalidInventoryRequest,
                };
                return new(outcome, null, reservation.Message);
            }
            foreach (var group in request.ShoppingCart.GroupBy(item => new { item.MerchantId, item.DishId }))
            {
                _db.OrderDishes.Add(new OrderDish
                {
                    OrderId = order.Id,
                    MerchantId = group.Key.MerchantId,
                    DishId = group.Key.DishId,
                    DishNum = group.Sum(item => item.DishNum),
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CreateOrderResult(
                CreateOrderOutcome.Success,
                await BuildResponseAsync(order.Id, request.UserId, cancellationToken),
                null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    /// <summary>
    /// 按用户查询订单列表。参照旧 getOrders（通过 OrderUser 关联找到用户的订单）。
    /// </summary>
    public async Task<IReadOnlyList<OrderResponse>> ListByUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var orderIds = await _db.OrderUsers
            .AsNoTracking()
            .Where(orderUser => orderUser.UserId == userId)
            .Select(orderUser => orderUser.OrderId)
            .ToListAsync(cancellationToken);

        if (orderIds.Count == 0)
        {
            return System.Array.Empty<OrderResponse>();
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(order => orderIds.Contains(order.Id))
            .Include(order => order.OrderDishes)
            .OrderBy(order => order.Id)
            .ToListAsync(cancellationToken);

        var fulfillments = await _db.MerchantOrderFulfillments.AsNoTracking()
            .Where(item => orderIds.Contains(item.OrderId))
            .ToListAsync(cancellationToken);
        var deliveries = await _db.RiderDeliveries.AsNoTracking()
            .Where(item => orderIds.Contains(item.OrderId))
            .Join(_db.Riders.AsNoTracking(), delivery => delivery.RiderId, rider => rider.Id,
                (delivery, rider) => new { delivery.OrderId, delivery.Status, rider.Name, rider.PhoneNumber })
            .ToDictionaryAsync(item => item.OrderId, cancellationToken);

        return orders
            .Select(order =>
            {
                var states = fulfillments.Where(item => item.OrderId == order.Id).Select(item => item.Status).ToArray();
                var fulfillmentStatus = states.Length == 0 ? "Confirmed"
                    : states.All(item => item == MerchantFulfillmentStatus.Rejected) ? "Rejected"
                    : states.Where(item => item != MerchantFulfillmentStatus.Rejected).All(item => item == MerchantFulfillmentStatus.ReadyForDelivery) ? "ReadyForDelivery"
                    : states.Any(item => item == MerchantFulfillmentStatus.Preparing) ? "Preparing"
                    : states.Any(item => item == MerchantFulfillmentStatus.Accepted) ? "Accepted"
                    : "Confirmed";
                var delivery = deliveries.GetValueOrDefault(order.Id);
                return ToResponse(order, userId, fulfillmentStatus, delivery?.Status.ToString(), delivery?.Name, delivery?.PhoneNumber);
            })
            .ToList();
    }

    /// <summary>
    /// 取消订单。参照旧 deleteOrder（恢复库存 + 删除订单），
    /// 但增加状态守卫：仅待支付（Pending）订单可取消。
    /// </summary>
    public async Task<CancelOrderResult> CancelAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var ownsOrder = await _db.OrderUsers.AnyAsync(
                orderUser => orderUser.OrderId == orderId && orderUser.UserId == userId,
                cancellationToken);
            var order = ownsOrder
                ? await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
                : null;
            if (order is null)
            {
                return new CancelOrderResult(CancelOrderOutcome.NotFound, "订单不存在");
            }

            // 状态守卫：只有待支付订单允许取消。
            if (order.Status != OrderStatus.Pending)
            {
                return new CancelOrderResult(
                    CancelOrderOutcome.NotCancellable,
                    "只能取消待支付订单");
            }

            var release = await _inventory.ReleaseAsync(orderId, null, cancellationToken);
            if (!release.Succeeded && release.Outcome != InventoryOutcome.ReservationNotFound)
                return new(CancelOrderOutcome.NotCancellable, release.Message);
            // Compatibility for orders created before the reservation ledger existed.
            if (release.Outcome == InventoryOutcome.ReservationNotFound)
            {
                var orderDishes = await _db.OrderDishes
                    .Where(orderDish => orderDish.OrderId == orderId)
                    .ToListAsync(cancellationToken);

                foreach (var orderDish in orderDishes)
                {
                    var dish = await _db.Dishes
                        .FirstOrDefaultAsync(
                            d => d.Id == orderDish.DishId && d.MerchantId == orderDish.MerchantId,
                            cancellationToken);
                    if (dish is not null)
                    {
                        dish.Inventory += orderDish.DishNum;
                    }
                }
            }

            // 删除订单，级联删除关联的五表记录。
            _db.Orders.Remove(order);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CancelOrderResult(CancelOrderOutcome.Success, null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<OrderResponse> BuildResponseAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderDishes)
            .FirstAsync(o => o.Id == orderId, cancellationToken);

        return ToResponse(order, userId);
    }

    private static OrderResponse ToResponse(
        Order order,
        int userId,
        string fulfillmentStatus = "Confirmed",
        string? deliveryStatus = null,
        string? riderName = null,
        string? riderPhone = null) => new(
        order.Id,
        userId,
        order.AddressId,
        order.Price,
        order.OrderTimestamp,
        order.Status,
        order.NeedUtensils,
        fulfillmentStatus,
        deliveryStatus,
        riderName,
        riderPhone,
        order.OrderDishes
            .Select(orderDish => new OrderDishResponse(
                orderDish.MerchantId,
                orderDish.DishId,
                orderDish.DishNum))
            .ToList());
}
