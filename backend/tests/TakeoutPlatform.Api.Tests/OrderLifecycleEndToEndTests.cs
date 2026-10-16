using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Order;
using TakeoutPlatform.Api.Features.User;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// Proves the final cross-role business flow through the real HTTP pipeline:
/// customer payment, administrator visibility, merchant fulfilment, rider
/// delivery, and the completed status shown back to the customer.
/// </summary>
public sealed class OrderLifecycleEndToEndTests
{
    private ApiTestFactory _factory = null!;
    private HttpClient _customer = null!;
    private HttpClient _administrator = null!;
    private HttpClient _merchant = null!;
    private HttpClient _rider = null!;
    private int _addressId;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
        _customer = _factory.CreateAuthenticatedClient(AccountRole.Customer, 1, 101);
        _administrator = _factory.CreateAuthenticatedClient(AccountRole.Administrator, 0, 1000);
        _merchant = _factory.CreateAuthenticatedClient(AccountRole.Merchant, 1, 201);
        _rider = _factory.CreateAuthenticatedClient(AccountRole.Rider, 1, 301);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Dishes.Add(new Dish
        {
            Id = 81,
            MerchantId = 1,
            Name = "Lifecycle Bento",
            Price = 18m,
            Inventory = 10,
        });
        var address = new UserAddress
        {
            UserId = 1,
            Address = "8 Lifecycle Road ZoneA",
            ContactName = "Lifecycle Customer",
            PhoneNumber = "13800000000",
        };
        db.UserAddresses.Add(address);
        db.CartItems.Add(new CartItem
        {
            UserId = 1,
            MerchantId = 1,
            DishId = 81,
            DishNum = 2,
        });
        db.SaveChanges();
        _addressId = address.Id;
    }

    [TearDown]
    public void TearDown()
    {
        _customer.Dispose();
        _administrator.Dispose();
        _merchant.Dispose();
        _rider.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Paid_order_can_move_from_customer_checkout_to_completed_delivery()
    {
        var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(new { addressId = _addressId }),
        };
        checkoutRequest.Headers.Add("Idempotency-Key", "lifecycle-test-key-0001");
        var checkoutResponse = await _customer.SendAsync(checkoutRequest);
        var checkout = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(checkoutResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(checkout?.Data?.Status, Is.EqualTo((int)OrderStatus.Paid));
        var orderId = checkout!.Data!.OrderId;

        var adminResponse = await _administrator.GetAsync($"/api/admin/orders?userId=1&status=Paid");
        var adminOrders = await adminResponse.Content.ReadFromJsonAsync<ApiResponse<PagedOrdersDto>>();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(adminResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(adminOrders?.Data?.Items.Any(item => item.OrderId == orderId), Is.True);
        }));

        var merchantOrders = await _merchant.GetAsync("/api/merchant/orders");
        var merchantBody = await merchantOrders.Content.ReadFromJsonAsync<ApiResponse<List<MerchantOrderDto>>>();
        Assert.That(merchantBody?.Data?.Single(item => item.OrderId == orderId).Status, Is.EqualTo("Confirmed"));

        await AssertSuccessfulTransition(_merchant, $"/api/merchant/orders/{orderId}/status", "Accept", "Accepted");
        await AssertSuccessfulTransition(_merchant, $"/api/merchant/orders/{orderId}/status", "StartPreparing", "Preparing");
        await AssertSuccessfulTransition(_merchant, $"/api/merchant/orders/{orderId}/status", "MarkReady", "ReadyForDelivery");

        var availableResponse = await _rider.GetAsync("/api/rider/available");
        var available = await availableResponse.Content.ReadFromJsonAsync<ApiResponse<List<AvailableOrderDto>>>();
        Assert.That(available?.Data?.Any(item => item.OrderId == orderId), Is.True);

        var acceptResponse = await _rider.PostAsync($"/api/rider/deliveries/{orderId}/accept", null);
        var accepted = await acceptResponse.Content.ReadFromJsonAsync<ApiResponse<StatusDto>>();
        Assert.That(accepted?.Data?.Status, Is.EqualTo("Assigned"));

        await AssertSuccessfulTransition(_rider, $"/api/rider/deliveries/{orderId}/status", "PickUp", "PickedUp");
        await AssertSuccessfulTransition(_rider, $"/api/rider/deliveries/{orderId}/status", "StartDelivering", "Delivering");
        await AssertSuccessfulTransition(_rider, $"/api/rider/deliveries/{orderId}/status", "Complete", "Completed");

        var customerOrdersResponse = await _customer.GetAsync("/api/orders/user/1");
        var customerOrders = await customerOrdersResponse.Content.ReadFromJsonAsync<ApiResponse<List<CustomerOrderDto>>>();
        Assert.That(customerOrders?.Data?.Single(item => item.Id == orderId).Status,
            Is.EqualTo((int)OrderStatus.Completed));

        var finalAdminResponse = await _administrator.GetAsync($"/api/admin/orders?status=Completed");
        var finalAdminOrders = await finalAdminResponse.Content.ReadFromJsonAsync<ApiResponse<PagedOrdersDto>>();
        Assert.That(finalAdminOrders?.Data?.Items.Any(item => item.OrderId == orderId), Is.True);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(verificationDb.Orders.Single(item => item.Id == orderId).Status,
                Is.EqualTo(OrderStatus.Completed));
            Assert.That(verificationDb.Dishes.Single(item => item.Id == 81).Inventory, Is.EqualTo(8));
            Assert.That(verificationDb.Payments.Count(item => item.OrderId == orderId), Is.EqualTo(1));
        }));
    }

    private static async Task AssertSuccessfulTransition(
        HttpClient client,
        string path,
        string action,
        string expectedStatus)
    {
        var response = await client.PutAsJsonAsync(path, new { action });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StatusDto>>();
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), responseText);
            Assert.That(body?.Data?.Status, Is.EqualTo(expectedStatus));
        }));
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message, string? Code);
    private sealed record CheckoutDto(int OrderId, int Status);
    private sealed record PagedOrdersDto(List<AdminOrderDto> Items, int Total);
    private sealed record AdminOrderDto(int OrderId, string Status);
    private sealed record MerchantOrderDto(int OrderId, string Status);
    private sealed record AvailableOrderDto(int OrderId);
    private sealed record StatusDto(int OrderId, string Status);
    private sealed record CustomerOrderDto(int Id, int Status);
}
