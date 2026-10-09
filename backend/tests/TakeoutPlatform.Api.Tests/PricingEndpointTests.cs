using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Tests;

public class PricingEndpointTests
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
    public async Task Quote_returns_server_calculated_breakdown_and_coupon()
    {
        SeedCart();

        var response = await _client.PostAsJsonAsync(
            "/api/pricing/quote", new { couponCode = "WELCOME5" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PricingDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.Success, Is.True);
        Assert.That(body.Data!.Subtotal, Is.EqualTo(40m));
        Assert.That(body.Data.MerchantDiscount, Is.EqualTo(5m));
        Assert.That(body.Data.CouponDiscount, Is.EqualTo(5m));
        Assert.That(body.Data.DeliveryFee, Is.EqualTo(5m));
        Assert.That(body.Data.Total, Is.EqualTo(35m));
        Assert.That(body.Data.SelectedCoupon!.Code, Is.EqualTo("WELCOME5"));
    }

    [Test]
    public async Task Quote_returns_machine_readable_failure_code()
    {
        SeedCart();

        var response = await _client.PostAsJsonAsync(
            "/api/pricing/quote", new { couponCode = "SAVE12" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body!.Success, Is.False);
        Assert.That(body.Code, Is.EqualTo("MinimumSpendNotMet"));
    }

    [Test]
    public async Task Pricing_endpoints_require_customer_authentication()
    {
        using var anonymous = _factory.CreateClient();

        var coupons = await anonymous.GetAsync("/api/pricing/coupons");
        var quote = await anonymous.PostAsJsonAsync("/api/pricing/quote", new { });

        Assert.That(coupons.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(quote.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private void SeedCart()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Dishes.Add(new Dish
        {
            Id = 10,
            MerchantId = 1,
            Name = "Noodles",
            Price = 20m,
            Inventory = 5,
        });
        db.CartItems.Add(new CartItem
        {
            UserId = 1,
            MerchantId = 1,
            DishId = 10,
            DishNum = 2,
        });
        db.SpecialOffers.Add(new SpecialOffer
        {
            MerchantId = 1,
            MinPrice = 30m,
            AmountRemission = 5m,
        });
        db.SaveChanges();
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message, string? Code);
    private sealed record PricingDto(
        decimal Subtotal,
        decimal MerchantDiscount,
        decimal CouponDiscount,
        decimal DeliveryFee,
        decimal Total,
        CouponDto? SelectedCoupon);
    private sealed record CouponDto(int Id, string Code, string Name);
}
