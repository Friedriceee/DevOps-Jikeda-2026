using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class DishService
{
    private readonly AppDbContext _db;

    public DishService(AppDbContext db) => _db = db;

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

        var dish = new Dish { MerchantId = request.MerchantId };
        Apply(dish, request);

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

        return await _db.Dishes
            .AsNoTracking()
            .Where(dish => dish.MerchantId == merchantId)
            .OrderBy(dish => dish.Id)
            .Select(dish => new DishResponse(
                dish.Id,
                dish.MerchantId,
                dish.Name,
                dish.Price,
                dish.Category,
                dish.ImageUrl,
                dish.Inventory))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 编辑菜品（US-03）。菜品不存在，或不属于 <paramref name="request"/> 里的商家，统一返回「未找到」，
    /// 不区分两种情况，避免暴露其他商家的菜品 ID 是否存在。
    /// </summary>
    public async Task<(bool Found, DishResponse? Dish)> UpdateAsync(
        int dishId,
        UpdateDishRequest request,
        CancellationToken cancellationToken)
    {
        var dish = await _db.Dishes
            .FirstOrDefaultAsync(d => d.Id == dishId && d.MerchantId == request.MerchantId, cancellationToken);

        if (dish is null)
        {
            return (false, null);
        }

        Apply(dish, request);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, ToResponse(dish));
    }

    /// <summary>下架/删除菜品（US-04）。同 <see cref="UpdateAsync"/>，找不到或不属于该商家都算「未找到」。</summary>
    public async Task<bool> DeleteAsync(int dishId, int merchantId, CancellationToken cancellationToken)
    {
        var dish = await _db.Dishes
            .FirstOrDefaultAsync(d => d.Id == dishId && d.MerchantId == merchantId, cancellationToken);

        if (dish is null)
        {
            return false;
        }

        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static void Apply(Dish dish, DishRequestBase request)
    {
        dish.Name = request.Name!.Trim();
        dish.Price = request.Price;
        dish.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        dish.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        dish.Inventory = request.Inventory;
    }

    private static DishResponse ToResponse(Dish dish) => new(
        dish.Id,
        dish.MerchantId,
        dish.Name,
        dish.Price,
        dish.Category,
        dish.ImageUrl,
        dish.Inventory);
}
