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

        var dish = new Dish
        {
            MerchantId = request.MerchantId,
            Name = request.Name!.Trim(),
            Price = request.Price,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Inventory = request.Inventory,
        };

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

        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
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
