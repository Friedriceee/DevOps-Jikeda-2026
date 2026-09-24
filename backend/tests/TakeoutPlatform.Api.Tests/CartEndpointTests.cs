using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.Merchant;
using Customer = TakeoutPlatform.Api.Features.User.User;

namespace TakeoutPlatform.Api.Tests;

public class CartEndpointTests
{
    private ApiTestFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
        _client = _factory.CreateAuthenticatedClient(AccountRole.Customer, 1);
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Add_get_update_and_delete_cart_item_returns_current_server_calculated_cart()
    {
        SeedDish(new Dish
        {
            Id = 10,
            MerchantId = 1,
            Name = "Noodles",
            Price = 12.50m,
            Inventory = 5,
        });

        var add = await _client.PostAsJsonAsync("/api/cart/items", new { dishId = 10, dishNum = 2 });
        var added = await add.Content.ReadFromJsonAsync<ApiResponse<CartDto>>();
        var itemId = added!.Data!.Merchants[0].Items[0].Id;

        var update = await _client.PutAsJsonAsync(
            $"/api/cart/items/{itemId}", new { dishNum = 3 });
        var updated = await update.Content.ReadFromJsonAsync<ApiResponse<CartDto>>();

        var delete = await _client.DeleteAsync($"/api/cart/items/{itemId}");
        var deleted = await delete.Content.ReadFromJsonAsync<ApiResponse<CartDto>>();

        Assert.That(add.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(added.Data.Total, Is.EqualTo(25m));
        Assert.That(update.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(updated?.Data?.Merchants[0].Items[0].DishNum, Is.EqualTo(3));
        Assert.That(updated?.Data?.Total, Is.EqualTo(37.50m));
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(deleted?.Data?.Merchants, Is.Empty);
    }

    [Test]
    public async Task Get_cart_groups_by_merchant_and_applies_best_eligible_offer()
    {
        SeedDish(new Dish { Id = 10, MerchantId = 1, Name = "A", Price = 20m, Inventory = 5 });
        SeedDish(new Dish { Id = 11, MerchantId = 2, Name = "B", Price = 10m, Inventory = 5 });
        SeedOffer(new SpecialOffer { MerchantId = 1, MinPrice = 30m, AmountRemission = 5m });

        await _client.PostAsJsonAsync("/api/cart/items", new { dishId = 10, dishNum = 2 });
        await _client.PostAsJsonAsync("/api/cart/items", new { dishId = 11, dishNum = 1 });
        var response = await _client.GetAsync("/api/cart");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CartDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Data?.Merchants, Has.Count.EqualTo(2));
        Assert.That(body?.Data?.Subtotal, Is.EqualTo(50m));
        Assert.That(body?.Data?.Discount, Is.EqualTo(5m));
        Assert.That(body?.Data?.Total, Is.EqualTo(45m));
    }

    [Test]
    public async Task Add_item_above_inventory_returns_bad_request()
    {
        SeedDish(new Dish { Id = 10, MerchantId = 1, Name = "Limited", Price = 12m, Inventory = 1 });

        var response = await _client.PostAsJsonAsync("/api/cart/items", new { dishId = 10, dishNum = 2 });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body?.Success, Is.False);
        Assert.That(body?.Message, Does.Contain("库存"));
    }

    [Test]
    public async Task Customer_cannot_update_another_customers_cart_item()
    {
        SeedDish(new Dish { Id = 10, MerchantId = 1, Name = "Private", Price = 10m, Inventory = 3 });
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new Customer { Id = 2, UserName = "Other Customer" });
            db.CartItems.Add(new CartItem
            {
                Id = 20,
                UserId = 2,
                MerchantId = 1,
                DishId = 10,
                DishNum = 1,
            });
            db.SaveChanges();
        }

        var response = await _client.PutAsJsonAsync("/api/cart/items/20", new { dishNum = 2 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Anonymous_cart_request_returns_unauthorized()
    {
        using var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/cart");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private void SeedDish(Dish dish)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Dishes.Add(dish);
        db.SaveChanges();
    }

    private void SeedOffer(SpecialOffer offer)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SpecialOffers.Add(offer);
        db.SaveChanges();
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record CartDto(
        List<MerchantGroupDto> Merchants,
        int TotalCount,
        decimal Subtotal,
        decimal Discount,
        decimal Total);
    private sealed record MerchantGroupDto(
        int MerchantId,
        string MerchantName,
        List<CartItemDto> Items,
        decimal Subtotal,
        decimal Discount,
        decimal Total);
    private sealed record CartItemDto(int Id, int MerchantId, int DishId, string DishName, decimal UnitPrice, int DishNum, decimal LineTotal, string? ImageUrl);
}
