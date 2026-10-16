using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Order;
using TakeoutPlatform.Api.Features.User;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// 骑手配送接口的集成测试（走完整 HTTP 管线 + JWT 鉴权）。
/// 覆盖：匿名 401、非骑手角色 403、骑手接单 happy path。
/// </summary>
public class RiderEndpointTests
{
    private ApiTestFactory _factory = null!;

    private const int ActiveRiderId = 1; // 种子骑手 ZoneA active available

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    /// <summary>用 factory 的服务作用域造一个 ReadyForDelivery 订单，返回 orderId。</summary>
    private int SeedReadyOrder()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var address = new UserAddress
        {
            UserId = 1,
            Address = "ZoneA Main Street 1",
            ContactName = "Customer",
            PhoneNumber = "13800000000",
        };
        db.UserAddresses.Add(address);
        db.SaveChanges();

        var order = new Order
        {
            Price = 50m,
            OrderTimestamp = DateTime.UtcNow,
            Status = OrderStatus.ReadyForDelivery,
            NeedUtensils = 1,
            AddressId = address.Id,
        };
        db.Orders.Add(order);
        db.SaveChanges();

        db.OrderUsers.Add(new OrderUser { OrderId = order.Id, UserId = 1 });
        if (!db.Dishes.Any(item => item.Id == 901))
            db.Dishes.Add(new TakeoutPlatform.Api.Features.Merchant.Dish { Id = 901, MerchantId = 1, Name = "Endpoint dish", Price = 50m, Inventory = 10 });
        db.OrderDishes.Add(new OrderDish { OrderId = order.Id, MerchantId = 1, DishId = 901, DishNum = 1 });
        db.MerchantOrderFulfillments.Add(new MerchantOrderFulfillment
        {
            OrderId = order.Id,
            MerchantId = 1,
            Status = MerchantFulfillmentStatus.ReadyForDelivery,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        return order.Id;
    }

    [Test]
    public async Task Anonymous_request_to_rider_endpoint_returns_401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/rider/available");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Customer_request_to_rider_endpoint_returns_403()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Customer, 1);
        var response = await client.GetAsync("/api/rider/available");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Rider_can_list_available_deliveries()
    {
        SeedReadyOrder();
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Rider, ActiveRiderId);

        var response = await client.GetAsync("/api/rider/available");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AvailableDto>>>();
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Rider_can_accept_a_ready_order()
    {
        var orderId = SeedReadyOrder();
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Rider, ActiveRiderId);

        var response = await client.PostAsync($"/api/rider/deliveries/{orderId}/accept", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DeliveryDto>>();
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.OrderId, Is.EqualTo(orderId));
        Assert.That(body?.Data?.Status, Is.EqualTo("Assigned"));
    }

    [Test]
    public async Task Rider_accepting_missing_order_returns_404()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Rider, ActiveRiderId);
        var response = await client.PostAsync("/api/rider/deliveries/99999/accept", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record AvailableDto(int OrderId, string Zone, decimal Price);
    private sealed record DeliveryDto(int OrderId, int RiderId, string Status);
}
