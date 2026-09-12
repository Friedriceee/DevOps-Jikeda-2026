using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Tests;

public class MerchantRegistrationEndpointTests
{
    private ApiTestFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Register_persists_merchant_with_hashed_passwords_and_safe_response()
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/register", ValidRequest());
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RegisteredMerchant>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.Username, Is.EqualTo("newmerchant"));
        Assert.That(body?.Data?.Id, Is.GreaterThan(2));
        var json = await response.Content.ReadAsStringAsync();
        Assert.That(json, Does.Not.Contain("StrongPassword123"));
        Assert.That(json, Does.Not.Contain("WalletSecret123"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var merchant = await db.Merchants.SingleAsync(m => m.Username == "newmerchant");
        var hasher = new PasswordHasher<Merchant>();
        Assert.That(merchant.Name, Is.EqualTo("New Merchant"));
        Assert.That(merchant.Address, Is.EqualTo("12 Main Street"));
        Assert.That(merchant.CouponType, Is.Zero);
        Assert.That(merchant.Wallet, Is.Zero);
        Assert.That(hasher.VerifyHashedPassword(merchant, merchant.PasswordHash!, "StrongPassword123"),
            Is.Not.EqualTo(PasswordVerificationResult.Failed));
        Assert.That(hasher.VerifyHashedPassword(merchant, merchant.WalletPasswordHash!, "WalletSecret123"),
            Is.Not.EqualTo(PasswordVerificationResult.Failed));
    }

    [Test]
    public async Task Duplicate_username_returns_conflict_and_does_not_create_another_merchant()
    {
        await _client.PostAsJsonAsync("/api/merchant/register", ValidRequest());
        var response = await _client.PostAsJsonAsync("/api/merchant/register", ValidRequest());
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(body?.Success, Is.False);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.That(await db.Merchants.CountAsync(m => m.Username == "newmerchant"), Is.EqualTo(1));
    }

    [TestCase("", "StrongPassword123")]
    [TestCase("newmerchant", "short")]
    public async Task Invalid_credentials_return_bad_request(string username, string password)
    {
        var request = ValidRequest() with { Username = username, Password = password };
        var response = await _client.PostAsJsonAsync("/api/merchant/register", request);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body?.Success, Is.False);
    }

    private static RegistrationRequest ValidRequest() => new(
        "newmerchant", "StrongPassword123", "New Merchant", "12 Main Street",
        "12345678", "Chinese", 36000, 79200, "WalletSecret123");

    private sealed record RegistrationRequest(
        string Username, string Password, string MerchantName, string MerchantAddress,
        string Contact, string DishType, int TimeForOpenBusiness,
        int TimeForCloseBusiness, string WalletPassword);
    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record RegisteredMerchant(int Id, string Username, string MerchantName);
}
