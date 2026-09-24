using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

public class AuthorizationEndpointTests
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
    public async Task Anonymous_request_to_protected_endpoint_returns_401()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/merchant/dishes", DishPayload(1));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Customer_request_to_merchant_endpoint_returns_403()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Customer, 1);
        var response = await client.PostAsJsonAsync("/api/merchant/dishes", DishPayload(1));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Customer_cannot_read_another_customers_addresses()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Customer, 1);
        var response = await client.GetAsync("/api/user/2/addresses");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Merchant_identity_comes_from_token_instead_of_request_body()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Merchant, 1);
        var response = await client.PostAsJsonAsync("/api/merchant/dishes", DishPayload(2));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();
        Assert.That(body?.Data?.MerchantId, Is.EqualTo(1));
    }

    private static object DishPayload(int merchantId) => new
    {
        merchantId,
        name = "Authorized Dish",
        price = 10.00m,
        inventory = 5,
    };

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record DishDto(int Id, int MerchantId, string Name);
}
