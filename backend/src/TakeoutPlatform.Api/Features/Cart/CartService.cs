using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Features.Cart;

public enum CartWriteOutcome
{
    Success,
    UserNotFound,
    DishNotFound,
    CartItemNotFound,
    InsufficientStock,
}

public sealed record CartWriteResult(
    CartWriteOutcome Outcome,
    CartResponse? Cart,
    string? Message);

/// <summary>
/// Persistent shopping-cart service. The client supplies only a dish id and a
/// quantity; price, merchant ownership, discounts and totals are all derived
/// from server-side data.
/// </summary>
public sealed class CartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public async Task<CartResponse?> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var userExists = await _db.Users.AnyAsync(user => user.Id == userId, cancellationToken);
        return userExists ? await BuildResponseAsync(userId, cancellationToken) : null;
    }

    public async Task<CartWriteResult> AddAsync(
        int userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _db.Users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return new CartWriteResult(CartWriteOutcome.UserNotFound, null, "用户不存在");
        }

        var dish = await _db.Dishes
            .SingleOrDefaultAsync(item => item.Id == request.DishId, cancellationToken);
        if (dish is null)
        {
            return new CartWriteResult(CartWriteOutcome.DishNotFound, null, "菜品不存在");
        }

        var cartItem = await _db.CartItems.SingleOrDefaultAsync(
            item => item.UserId == userId && item.DishId == request.DishId,
            cancellationToken);
        var requestedQuantity = (cartItem?.DishNum ?? 0) + request.DishNum;
        if (requestedQuantity > dish.Inventory)
        {
            return new CartWriteResult(
                CartWriteOutcome.InsufficientStock,
                null,
                $"菜品 {dish.Name} 库存仅余 {dish.Inventory}");
        }

        if (cartItem is null)
        {
            _db.CartItems.Add(new CartItem
            {
                UserId = userId,
                MerchantId = dish.MerchantId,
                DishId = dish.Id,
                DishNum = request.DishNum,
            });
        }
        else
        {
            cartItem.DishNum = requestedQuantity;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CartWriteResult(
            CartWriteOutcome.Success,
            await BuildResponseAsync(userId, cancellationToken),
            null);
    }

    public async Task<CartWriteResult> UpdateAsync(
        int userId,
        int cartItemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var cartItem = await _db.CartItems
            .Include(item => item.Dish)
            .SingleOrDefaultAsync(
                item => item.Id == cartItemId && item.UserId == userId,
                cancellationToken);
        if (cartItem is null)
        {
            return new CartWriteResult(CartWriteOutcome.CartItemNotFound, null, "购物车商品不存在");
        }

        if (request.DishNum > cartItem.Dish.Inventory)
        {
            return new CartWriteResult(
                CartWriteOutcome.InsufficientStock,
                null,
                $"菜品 {cartItem.Dish.Name} 库存仅余 {cartItem.Dish.Inventory}");
        }

        cartItem.DishNum = request.DishNum;
        await _db.SaveChangesAsync(cancellationToken);
        return new CartWriteResult(
            CartWriteOutcome.Success,
            await BuildResponseAsync(userId, cancellationToken),
            null);
    }

    public async Task<CartWriteResult> DeleteItemAsync(
        int userId,
        int cartItemId,
        CancellationToken cancellationToken)
    {
        var cartItem = await _db.CartItems.SingleOrDefaultAsync(
            item => item.Id == cartItemId && item.UserId == userId,
            cancellationToken);
        if (cartItem is null)
        {
            return new CartWriteResult(CartWriteOutcome.CartItemNotFound, null, "购物车商品不存在");
        }

        _db.CartItems.Remove(cartItem);
        await _db.SaveChangesAsync(cancellationToken);
        return new CartWriteResult(
            CartWriteOutcome.Success,
            await BuildResponseAsync(userId, cancellationToken),
            null);
    }

    public async Task<CartWriteResult> DeleteMerchantItemsAsync(
        int userId,
        int merchantId,
        CancellationToken cancellationToken)
    {
        var items = await _db.CartItems
            .Where(item => item.UserId == userId && item.MerchantId == merchantId)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return new CartWriteResult(CartWriteOutcome.CartItemNotFound, null, "该商家的购物车为空");
        }

        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);
        return new CartWriteResult(
            CartWriteOutcome.Success,
            await BuildResponseAsync(userId, cancellationToken),
            null);
    }

    public async Task<CartResponse> ClearAsync(int userId, CancellationToken cancellationToken)
    {
        var items = await _db.CartItems
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);
        if (items.Count > 0)
        {
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await BuildResponseAsync(userId, cancellationToken);
    }

    private async Task<CartResponse> BuildResponseAsync(int userId, CancellationToken cancellationToken)
    {
        var rows = await _db.CartItems
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Dish.Merchant.Name)
            .ThenBy(item => item.Dish.Name)
            .ThenBy(item => item.Id)
            .Select(item => new CartRow(
                item.Id,
                item.MerchantId,
                item.DishId,
                item.Dish.Name,
                item.Dish.Price,
                item.DishNum,
                item.Dish.ImageUrl,
                item.Dish.Merchant.Name))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new CartResponse(Array.Empty<CartMerchantGroupResponse>(), 0, 0m, 0m, 0m);
        }

        var merchantIds = rows.Select(row => row.MerchantId).Distinct().ToList();
        var offers = await _db.SpecialOffers
            .AsNoTracking()
            .Where(offer => merchantIds.Contains(offer.MerchantId))
            .Select(offer => new OfferRow(offer.MerchantId, offer.MinPrice, offer.AmountRemission))
            .ToListAsync(cancellationToken);

        var groups = rows
            .GroupBy(row => new { row.MerchantId, row.MerchantName })
            .Select(group =>
            {
                var items = group.Select(row => new CartItemResponse(
                    row.Id,
                    row.MerchantId,
                    row.DishId,
                    row.DishName,
                    row.UnitPrice,
                    row.DishNum,
                    row.UnitPrice * row.DishNum,
                    row.ImageUrl)).ToList();
                var subtotal = items.Sum(item => item.LineTotal);
                var discount = offers
                    .Where(offer => offer.MerchantId == group.Key.MerchantId && offer.MinPrice <= subtotal)
                    .Select(offer => offer.AmountRemission)
                    .DefaultIfEmpty(0m)
                    .Max();
                discount = Math.Min(discount, subtotal);

                return new CartMerchantGroupResponse(
                    group.Key.MerchantId,
                    group.Key.MerchantName,
                    items,
                    subtotal,
                    discount,
                    subtotal - discount);
            })
            .ToList();

        return new CartResponse(
            groups,
            rows.Sum(row => row.DishNum),
            groups.Sum(group => group.Subtotal),
            groups.Sum(group => group.Discount),
            groups.Sum(group => group.Total));
    }

    private sealed record CartRow(
        int Id,
        int MerchantId,
        int DishId,
        string DishName,
        decimal UnitPrice,
        int DishNum,
        string? ImageUrl,
        string MerchantName);

    private sealed record OfferRow(int MerchantId, decimal MinPrice, decimal AmountRemission);
}
