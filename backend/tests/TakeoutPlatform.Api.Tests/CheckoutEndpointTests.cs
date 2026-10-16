using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.Merchant;
using Customer = TakeoutPlatform.Api.Features.User.User;
using UserAddress = TakeoutPlatform.Api.Features.User.UserAddress;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// Checkout and simulated payment through the full HTTP pipeline.
///
/// Fixture data: customer 1 (seeded) with one address, a 10.00 dish from merchant 1
/// (stock 20) in the cart with quantity 4, so the food subtotal is 40.00. The flat
/// delivery fee is 5.00, so the server total without discounts is 45.00.
/// </summary>
public class CheckoutEndpointTests
{
    private const int UserId = 1;
    private const int DishId = 10;
    private const decimal ExpectedTotal = 45.00m;

    private ApiTestFactory _factory = null!;
    private HttpClient _customer = null!;
    private int _addressId;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
        _customer = _factory.CreateAuthenticatedClient(AccountRole.Customer, UserId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Dishes.Add(new Dish { Id = DishId, MerchantId = 1, Name = "Noodles", Price = 10.00m, Inventory = 20 });
        var address = new UserAddress
        {
            UserId = UserId,
            Address = "1 Campus Road ZoneA",
            ContactName = "Test Customer",
            PhoneNumber = "13800000000",
        };
        db.UserAddresses.Add(address);
        db.CartItems.Add(new CartItem { UserId = UserId, MerchantId = 1, DishId = DishId, DishNum = 4 });
        db.SaveChanges();
        _addressId = address.Id;
    }

    [TearDown]
    public void TearDown()
    {
        _customer.Dispose();
        _factory.Dispose();
    }

    // ---------- happy path ----------

    [Test]
    public async Task Checkout_creates_a_paid_order_priced_by_the_server()
    {
        var response = await Checkout(Key(), new { addressId = _addressId });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body!.Success, Is.True);
        Assert.That(body.Data!.Status, Is.EqualTo(PaidStatus));
        Assert.That(body.Data.AmountPaid, Is.EqualTo(ExpectedTotal));
        Assert.That(body.Data.Replayed, Is.False);
        Assert.That(body.Data.PaymentReference, Does.StartWith("SIM-"));
        Assert.That(body.Data.Pricing.Subtotal, Is.EqualTo(40.00m));
        Assert.That(body.Data.Pricing.DeliveryFee, Is.EqualTo(5.00m));
        Assert.That(body.Data.Dishes, Has.Count.EqualTo(1));

        using var db = Db();
        var order = await db.Orders.Include(o => o.OrderDishes).SingleAsync();
        Assert.That(order.Price, Is.EqualTo(ExpectedTotal));
        Assert.That(order.AddressId, Is.EqualTo(_addressId));
        Assert.That((int)order.Status, Is.EqualTo(PaidStatus));
        Assert.That(await db.OrderUsers.SingleAsync(), Has.Property("UserId").EqualTo(UserId));
        Assert.That((await db.OrderRiders.SingleAsync()).RiderPrice, Is.EqualTo(5.00m));
        Assert.That((await db.Payments.SingleAsync()).Amount, Is.EqualTo(ExpectedTotal));
    }

    [Test]
    public async Task Checkout_reserves_stock_and_clears_the_cart()
    {
        await Checkout(Key(), new { addressId = _addressId });

        using var db = Db();
        Assert.That((await db.Dishes.SingleAsync(d => d.Id == DishId)).Inventory, Is.EqualTo(16));
        Assert.That(await db.CartItems.CountAsync(), Is.Zero);
        Assert.That(await db.InventoryReservations.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Checkout_applies_a_valid_platform_coupon_and_records_it_on_the_order()
    {
        var response = await Checkout(Key(), new { addressId = _addressId, couponCode = "welcome5" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body!.Data!.AmountPaid, Is.EqualTo(40.00m)); // 40 food - 5 coupon + 5 delivery
        Assert.That(body.Data.Pricing.CouponDiscount, Is.EqualTo(5.00m));

        using var db = Db();
        Assert.That(await db.OrderCoupons.CountAsync(), Is.EqualTo(1));
        Assert.That((await db.CustomerCoupons.SingleAsync(c => c.UserId == UserId && c.CouponId == 1)).UsedQuantity,
            Is.EqualTo(1));
    }

    [Test]
    public async Task Replaying_checkout_does_not_consume_the_coupon_twice()
    {
        var key = Key();
        var first = await Checkout(key, new { addressId = _addressId, couponCode = "WELCOME5" });
        var replay = await Checkout(key, new { addressId = _addressId, couponCode = "WELCOME5" });

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var db = Db();
        var allocation = await db.CustomerCoupons.SingleAsync(c => c.UserId == UserId && c.CouponId == 1);
        Assert.That(allocation.UsedQuantity, Is.EqualTo(1));
        Assert.That(await db.OrderCoupons.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task A_paid_order_is_visible_to_the_merchant_as_confirmed()
    {
        var checkout = await Checkout(Key(), new { addressId = _addressId });
        var paid = (await checkout.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>())!.Data!;

        using var merchant = _factory.CreateAuthenticatedClient(AccountRole.Merchant, 1);
        var orders = await merchant.GetAsync("/api/merchant/orders");
        var text = await orders.Content.ReadAsStringAsync();

        Assert.That(orders.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(text, Does.Contain($"\"orderId\":{paid.OrderId}"));
    }

    // ---------- authentication and authorization ----------

    [Test]
    public async Task Anonymous_checkout_is_unauthorized()
    {
        using var anonymous = _factory.CreateClient();
        var response = await Send(anonymous, Key(), new { addressId = _addressId });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase(AccountRole.Merchant)]
    [TestCase(AccountRole.Rider)]
    [TestCase(AccountRole.Administrator)]
    public async Task Only_customers_can_check_out(AccountRole role)
    {
        using var client = _factory.CreateAuthenticatedClient(role, 1);
        var response = await Send(client, Key(), new { addressId = _addressId });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        using var db = Db();
        Assert.That(await db.Orders.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Another_customers_address_cannot_be_used()
    {
        int foreignAddress;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new Customer { Id = 2, UserName = "Other" });
            var other = new UserAddress
            {
                UserId = 2, Address = "9 Other Street", ContactName = "Other", PhoneNumber = "13900000000",
            };
            db.UserAddresses.Add(other);
            db.SaveChanges();
            foreignAddress = other.Id;
        }

        var response = await Checkout(Key(), new { addressId = foreignAddress });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body!.Code, Is.EqualTo("AddressNotFound"));
        using var check = Db();
        Assert.That(await check.CartItems.CountAsync(), Is.EqualTo(1));
    }

    // ---------- the client cannot influence the price or identity ----------

    [Test]
    public async Task Client_supplied_totals_items_and_user_ids_are_ignored()
    {
        var response = await Checkout(Key(), new
        {
            addressId = _addressId,
            total = 0.01m,
            price = 0.01m,
            amount = 0.01m,
            riderPrice = 0m,
            userId = 999,
            shoppingCart = new[] { new { merchantId = 1, dishId = DishId, dishNum = 1 } },
        });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body!.Data!.AmountPaid, Is.EqualTo(ExpectedTotal));
        Assert.That(body.Data.Dishes.Single().DishNum, Is.EqualTo(4));

        using var db = Db();
        Assert.That((await db.Orders.SingleAsync()).Price, Is.EqualTo(ExpectedTotal));
        Assert.That((await db.OrderUsers.SingleAsync()).UserId, Is.EqualTo(UserId));
    }

    [Test]
    public async Task Price_changes_after_the_cart_was_filled_are_charged_at_the_current_price()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Dishes.SingleAsync(d => d.Id == DishId)).Price = 12.00m;
            db.SaveChanges();
        }

        var response = await Checkout(Key(), new { addressId = _addressId });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(body!.Data!.AmountPaid, Is.EqualTo(53.00m)); // 4 x 12 + 5
    }

    // ---------- idempotency ----------

    [Test]
    public async Task Repeating_a_request_returns_the_same_order_without_a_second_charge_or_stock_change()
    {
        var key = Key();
        var first = await Checkout(key, new { addressId = _addressId });
        var second = await Checkout(key, new { addressId = _addressId });
        var third = await Checkout(key, new { addressId = _addressId });

        var created = (await first.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>())!.Data!;
        var replay = (await second.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>())!.Data!;
        var replayAgain = (await third.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>())!.Data!;

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(third.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(replay.Replayed, Is.True);
        Assert.That(replay.OrderId, Is.EqualTo(created.OrderId));
        Assert.That(replayAgain.OrderId, Is.EqualTo(created.OrderId));
        Assert.That(replay.PaymentReference, Is.EqualTo(created.PaymentReference));
        Assert.That(replay.AmountPaid, Is.EqualTo(created.AmountPaid));

        using var db = Db();
        Assert.That(await db.Orders.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.Payments.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.InventoryReservations.CountAsync(), Is.EqualTo(1));
        Assert.That((await db.Dishes.SingleAsync(d => d.Id == DishId)).Inventory, Is.EqualTo(16));
    }

    [Test]
    public async Task A_replay_still_works_after_the_customer_refilled_the_cart()
    {
        var key = Key();
        var first = await Checkout(key, new { addressId = _addressId });
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CartItems.Add(new CartItem { UserId = UserId, MerchantId = 1, DishId = DishId, DishNum = 1 });
            db.SaveChanges();
        }

        var second = await Checkout(key, new { addressId = _addressId });
        var created = (await first.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>())!.Data!;
        var replay = (await second.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>())!.Data!;

        Assert.That(replay.OrderId, Is.EqualTo(created.OrderId));
        using var check = Db();
        Assert.That(await check.CartItems.CountAsync(), Is.EqualTo(1), "the new cart must stay untouched");
        Assert.That(await check.Orders.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Reusing_a_key_with_a_different_request_is_rejected()
    {
        var key = Key();
        await Checkout(key, new { addressId = _addressId });

        var response = await Checkout(key, new { addressId = _addressId, couponCode = "WELCOME5" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(body!.Code, Is.EqualTo("IdempotencyKeyReused"));
        using var db = Db();
        Assert.That(await db.Orders.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task The_same_key_used_by_two_customers_creates_two_independent_orders()
    {
        var key = Key();
        await Checkout(key, new { addressId = _addressId });

        int secondAddress;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new Customer { Id = 2, UserName = "Second" });
            var address = new UserAddress
            {
                UserId = 2, Address = "2 Campus Road", ContactName = "Second", PhoneNumber = "13900000000",
            };
            db.UserAddresses.Add(address);
            db.CartItems.Add(new CartItem { UserId = 2, MerchantId = 1, DishId = DishId, DishNum = 1 });
            db.SaveChanges();
            secondAddress = address.Id;
        }

        using var second = _factory.CreateAuthenticatedClient(AccountRole.Customer, 2, accountId: 101);
        var response = await Send(second, key, new { addressId = secondAddress });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        using var check = Db();
        Assert.That(await check.Orders.CountAsync(), Is.EqualTo(2));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("short")]
    [TestCase("contains spaces in the key")]
    public async Task A_missing_or_malformed_idempotency_key_is_a_bad_request(string? key)
    {
        var response = await Send(_customer, key, new { addressId = _addressId });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body!.Code, Is.EqualTo("InvalidIdempotencyKey"));
        using var db = Db();
        Assert.That(await db.Orders.CountAsync(), Is.Zero);
    }

    // ---------- failure paths leave nothing behind ----------

    [Test]
    public async Task A_declined_payment_returns_402_and_rolls_back_stock_cart_and_order()
    {
        var response = await Checkout(
            Key(), new { addressId = _addressId, paymentMethod = "SimulatedCardDeclined" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.PaymentRequired));
        Assert.That(body!.Code, Is.EqualTo("PaymentDeclined"));

        using var db = Db();
        Assert.That(await db.Orders.CountAsync(), Is.Zero);
        Assert.That(await db.Payments.CountAsync(), Is.Zero);
        Assert.That(await db.InventoryReservations.CountAsync(), Is.Zero);
        Assert.That(await db.CartItems.CountAsync(), Is.EqualTo(1));
        Assert.That((await db.Dishes.SingleAsync(d => d.Id == DishId)).Inventory, Is.EqualTo(20));
    }

    [Test]
    public async Task After_a_decline_the_customer_can_pay_with_a_new_key()
    {
        await Checkout(Key(), new { addressId = _addressId, paymentMethod = "SimulatedCardDeclined" });

        var retry = await Checkout(Key(), new { addressId = _addressId });

        Assert.That(retry.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        using var db = Db();
        Assert.That((await db.Dishes.SingleAsync(d => d.Id == DishId)).Inventory, Is.EqualTo(16));
    }

    [Test]
    public async Task An_unknown_payment_method_is_rejected_before_anything_is_changed()
    {
        var response = await Checkout(Key(), new { addressId = _addressId, paymentMethod = "Bitcoin" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body!.Code, Is.EqualTo("UnsupportedPaymentMethod"));
        using var db = Db();
        Assert.That(await db.CartItems.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Insufficient_stock_returns_409_and_leaves_stock_and_cart_unchanged()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Dishes.SingleAsync(d => d.Id == DishId)).Inventory = 3;
            db.SaveChanges();
        }

        var response = await Checkout(Key(), new { addressId = _addressId });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(body!.Code, Is.EqualTo("InsufficientStock"));
        using var check = Db();
        Assert.That((await check.Dishes.SingleAsync(d => d.Id == DishId)).Inventory, Is.EqualTo(3));
        Assert.That(await check.Orders.CountAsync(), Is.Zero);
        Assert.That(await check.CartItems.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task An_empty_cart_is_a_bad_request()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CartItems.RemoveRange(db.CartItems);
            db.SaveChanges();
        }

        var response = await Checkout(Key(), new { addressId = _addressId });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body!.Code, Is.EqualTo("EmptyCart"));
    }

    [Test]
    public async Task A_coupon_below_its_minimum_spend_is_rejected_and_nothing_is_charged()
    {
        var response = await Checkout(Key(), new { addressId = _addressId, couponCode = "SAVE12" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body!.Code, Is.EqualTo("MinimumSpendNotMet"));
        using var db = Db();
        Assert.That(await db.Payments.CountAsync(), Is.Zero);
        Assert.That(await db.CartItems.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task An_invalid_address_id_is_a_validation_error()
    {
        var response = await Checkout(Key(), new { addressId = 0 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ---------- helpers ----------

    private const int PaidStatus = 1;

    private static string Key() => "key-" + Guid.NewGuid().ToString("N");

    private Task<HttpResponseMessage> Checkout(string key, object body) => Send(_customer, key, body);

    private static Task<HttpResponseMessage> Send(HttpClient client, string? key, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(body),
        };
        if (key is not null)
            request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        return client.SendAsync(request);
    }

    private AppDbContext Db() => _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message, string? Code);

    private sealed record CheckoutDto(
        int OrderId,
        int Status,
        decimal AmountPaid,
        int PaymentId,
        string PaymentReference,
        string PaymentMethod,
        bool Replayed,
        PricingDto Pricing,
        List<DishLineDto> Dishes);

    private sealed record PricingDto(
        decimal Subtotal, decimal MerchantDiscount, decimal CouponDiscount, decimal DeliveryFee, decimal Total);

    private sealed record DishLineDto(int MerchantId, int DishId, int DishNum);
}
