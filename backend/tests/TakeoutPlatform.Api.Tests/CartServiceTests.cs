using NUnit.Framework;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.Merchant;
using Customer = TakeoutPlatform.Api.Features.User.User;

namespace TakeoutPlatform.Api.Tests;

public class CartServiceTests : ServiceTestBase
{
    [Test]
    public async Task AddAsync_combines_same_dish_and_calculates_discounted_merchant_total()
    {
        await using var context = CreateContext();
        context.Dishes.Add(new Dish
        {
            Id = 10,
            MerchantId = MerchantId,
            Name = "Noodles",
            Price = 20m,
            Inventory = 5,
        });
        context.SpecialOffers.Add(new SpecialOffer
        {
            MerchantId = MerchantId,
            MinPrice = 30m,
            AmountRemission = 5m,
        });
        await context.SaveChangesAsync();

        var service = new CartService(context);
        await service.AddAsync(1, new AddCartItemRequest { DishId = 10, DishNum = 1 }, CancellationToken.None);
        var result = await service.AddAsync(1, new AddCartItemRequest { DishId = 10, DishNum = 2 }, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CartWriteOutcome.Success));
        Assert.That(result.Cart!.Merchants, Has.Count.EqualTo(1));
        Assert.That(result.Cart.Merchants[0].Items, Has.Count.EqualTo(1));
        Assert.That(result.Cart.Merchants[0].Items[0].DishNum, Is.EqualTo(3));
        Assert.That(result.Cart.Merchants[0].Subtotal, Is.EqualTo(60m));
        Assert.That(result.Cart.Merchants[0].Discount, Is.EqualTo(5m));
        Assert.That(result.Cart.Total, Is.EqualTo(55m));
    }

    [Test]
    public async Task GetAsync_groups_items_by_merchant_and_uses_server_side_prices()
    {
        await using var context = CreateContext();
        context.Dishes.AddRange(
            new Dish { Id = 10, MerchantId = MerchantId, Name = "A", Price = 12m, Inventory = 5 },
            new Dish { Id = 11, MerchantId = OtherMerchantId, Name = "B", Price = 8m, Inventory = 5 });
        await context.SaveChangesAsync();

        var service = new CartService(context);
        await service.AddAsync(1, new AddCartItemRequest { DishId = 10, DishNum = 2 }, CancellationToken.None);
        await service.AddAsync(1, new AddCartItemRequest { DishId = 11, DishNum = 1 }, CancellationToken.None);

        var cart = await service.GetAsync(1, CancellationToken.None);

        Assert.That(cart, Is.Not.Null);
        Assert.That(cart!.Merchants.Select(group => group.MerchantId),
            Is.EquivalentTo(new[] { MerchantId, OtherMerchantId }));
        Assert.That(cart.TotalCount, Is.EqualTo(3));
        Assert.That(cart.Subtotal, Is.EqualTo(32m));
        Assert.That(cart.Total, Is.EqualTo(32m));
    }

    [Test]
    public async Task AddAsync_rejects_quantity_above_current_inventory()
    {
        await using var context = CreateContext();
        context.Dishes.Add(new Dish
        {
            Id = 10,
            MerchantId = MerchantId,
            Name = "Limited",
            Price = 10m,
            Inventory = 2,
        });
        await context.SaveChangesAsync();

        var result = await new CartService(context).AddAsync(
            1,
            new AddCartItemRequest { DishId = 10, DishNum = 3 },
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CartWriteOutcome.InsufficientStock));
        Assert.That(context.CartItems, Is.Empty);
    }

    [Test]
    public async Task UpdateAndDelete_only_affect_the_authenticated_customers_item()
    {
        await using var context = CreateContext();
        context.Users.Add(new Customer { Id = 2, UserName = "Another Customer" });
        context.Dishes.Add(new Dish
        {
            Id = 10,
            MerchantId = MerchantId,
            Name = "Owned Elsewhere",
            Price = 10m,
            Inventory = 5,
        });
        context.CartItems.Add(new CartItem
        {
            Id = 20,
            UserId = 2,
            MerchantId = MerchantId,
            DishId = 10,
            DishNum = 1,
        });
        await context.SaveChangesAsync();

        var service = new CartService(context);
        var update = await service.UpdateAsync(
            1, 20, new UpdateCartItemRequest { DishNum = 2 }, CancellationToken.None);
        var delete = await service.DeleteItemAsync(1, 20, CancellationToken.None);

        Assert.That(update.Outcome, Is.EqualTo(CartWriteOutcome.CartItemNotFound));
        Assert.That(delete.Outcome, Is.EqualTo(CartWriteOutcome.CartItemNotFound));
        Assert.That(context.CartItems.Single().DishNum, Is.EqualTo(1));
    }
}
