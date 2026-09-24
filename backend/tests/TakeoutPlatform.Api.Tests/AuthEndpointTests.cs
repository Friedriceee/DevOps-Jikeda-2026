using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

public class AuthEndpointTests
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
    public async Task Customer_can_register_and_login_with_required_jwt_claims()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/customer/register", CustomerPayload());
        Assert.That(register.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "customer-a",
            password = "SecurePassword123",
        });
        var body = await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Data?.Role, Is.EqualTo("Customer"));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.Data!.AccessToken);
        Assert.That(token.Claims.Any(c => c.Type == "sub" && c.Value == body.Data.AccountId.ToString()), Is.True);
        Assert.That(token.Claims.Any(c => c.Type == "role" && c.Value == "Customer"), Is.True);
        Assert.That(token.Claims.Any(c => c.Type == "profile_id" && c.Value == body.Data.ProfileId.ToString()), Is.True);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", body.Data.AccessToken);
        var protectedResponse = await _client.GetAsync(
            $"/api/user/{body.Data.ProfileId}/addresses");
        Assert.That(protectedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Login_with_wrong_password_returns_401_without_token()
    {
        await _client.PostAsJsonAsync("/api/auth/customer/register", CustomerPayload());
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "customer-a",
            password = "wrong-password",
        });
        var body = await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(body?.Success, Is.False);
        Assert.That(body?.Data, Is.Null);
    }

    private static object CustomerPayload() => new
    {
        username = "customer-a",
        password = "SecurePassword123",
        displayName = "Customer A",
        phoneNumber = "13800000001",
    };

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
}
