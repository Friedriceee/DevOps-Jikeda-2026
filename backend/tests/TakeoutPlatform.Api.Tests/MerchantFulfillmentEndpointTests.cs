using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Order;

namespace TakeoutPlatform.Api.Tests;

public class MerchantFulfillmentEndpointTests
{
    private ApiTestFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task Merchant_can_list_and_accept_an_owned_confirmed_order()
    {
        var orderId = await SeedConfirmedOrderAsync();
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Merchant, 1);

        var listResponse = await client.GetAsync("/api/merchant/orders");
        var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<MerchantOrderDto>>>();
        var transitionResponse = await client.PutAsJsonAsync(
            $"/api/merchant/orders/{orderId}/status",
            new { action = "Accept" });
        var transitionBody = await transitionResponse.Content.ReadFromJsonAsync<ApiResponse<MerchantOrderDto>>();

        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(listBody?.Data, Has.Count.EqualTo(1));
        Assert.That(listBody?.Data?[0].Status, Is.EqualTo("Confirmed"));
        Assert.That(transitionResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(transitionBody?.Data?.Status, Is.EqualTo("Accepted"));
    }

    [Test]
    public async Task Customer_role_is_forbidden_from_merchant_fulfilment()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Customer, 1);
        var response = await client.GetAsync("/api/merchant/orders");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Wrong_merchant_is_forbidden_from_transitioning_the_order()
    {
        var orderId = await SeedConfirmedOrderAsync();
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Merchant, 2);

        var response = await client.PutAsJsonAsync(
            $"/api/merchant/orders/{orderId}/status",
            new { action = "Accept" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Invalid_transition_returns_conflict()
    {
        var orderId = await SeedConfirmedOrderAsync();
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Merchant, 1);

        var response = await client.PutAsJsonAsync(
            $"/api/merchant/orders/{orderId}/status",
            new { action = "MarkReady" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    private async Task<int> SeedConfirmedOrderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dish = new Dish
        {
            MerchantId = 1,
            Name = "API fulfilment dish",
            Price = 18m,
            Inventory = 8,
        };
        db.Dishes.Add(dish);
        await db.SaveChangesAsync();

        var order = new Order
        {
            Price = 36m,
            OrderTimestamp = DateTime.UtcNow,
            AddressId = 1,
            Status = OrderStatus.Paid,
            OrderUser = new OrderUser { UserId = 1 },
            OrderDishes =
            [
                new OrderDish
                {
                    MerchantId = 1,
                    DishId = dish.Id,
                    DishNum = 2,
                },
            ],
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record MerchantOrderDto(int OrderId, string Status);
}
