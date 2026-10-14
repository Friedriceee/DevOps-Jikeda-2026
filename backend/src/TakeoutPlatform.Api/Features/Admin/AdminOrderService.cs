using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Order;

namespace TakeoutPlatform.Api.Features.Admin;

/// <summary>
/// 管理员订单查询：按用户、状态、时间范围筛选并分页。
/// 只读查询，不修改订单，天然不影响已支付订单的价格快照。
/// </summary>
public sealed class AdminOrderService
{
    private readonly AppDbContext _db;

    public AdminOrderService(AppDbContext db) => _db = db;

    public async Task<PagedResult<AdminOrderResponse>> QueryAsync(
        int? userId,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        // 订单-用户映射（通过 OrderUser 关联）。
        var query =
            from order in _db.Orders.AsNoTracking()
            join orderUser in _db.OrderUsers.AsNoTracking() on order.Id equals orderUser.OrderId
            select new { order, orderUser.UserId };

        if (userId is > 0)
            query = query.Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            query = query.Where(x => x.order.Status == parsedStatus);

        if (fromUtc is not null)
            query = query.Where(x => x.order.OrderTimestamp >= fromUtc);

        if (toUtc is not null)
            query = query.Where(x => x.order.OrderTimestamp <= toUtc);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.order.OrderTimestamp)
            .ThenByDescending(x => x.order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.order.Id,
                x.UserId,
                x.order.Status,
                x.order.Price,
                x.order.OrderTimestamp,
                ItemCount = x.order.OrderDishes.Count,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new AdminOrderResponse(
                r.Id, r.UserId, r.Status.ToString(), r.Price, r.OrderTimestamp, r.ItemCount))
            .ToList();

        return new PagedResult<AdminOrderResponse>(items, page, pageSize, total);
    }
}
