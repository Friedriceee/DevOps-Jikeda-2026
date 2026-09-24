using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// 地址接口的集成测试（走完整 HTTP 管线）。
/// 覆盖：新增地址、按用户查询、用户不存在、请求校验（手机号格式）。
/// 用户 Id=1 来自种子数据。
/// </summary>
public class AddressEndpointTests
{
    private ApiTestFactory _factory = null!;
    private HttpClient _client = null!;

    private const int UserId = 1;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
        _client = _factory.CreateAuthenticatedClient(AccountRole.Customer, UserId);
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Create_address_returns_created_address()
    {
        var response = await _client.PostAsJsonAsync("/api/user/addresses", new
        {
            userId = UserId,
            address = "No.1 Main Street",
            houseNumber = "101",
            contactName = "Alice",
            phoneNumber = "13800000000",
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AddressDto>>();
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.ContactName, Is.EqualTo("Alice"));
        Assert.That(body?.Data?.UserId, Is.EqualTo(UserId));
    }

    [Test]
    public async Task List_addresses_returns_created_addresses()
    {
        await _client.PostAsJsonAsync("/api/user/addresses", new
        {
            userId = UserId,
            address = "No.2 Side Road",
            contactName = "Bob",
            phoneNumber = "13900000000",
        });

        var response = await _client.GetAsync($"/api/user/{UserId}/addresses");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AddressDto>>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data, Has.Count.EqualTo(1));
        Assert.That(body?.Data?[0].Address, Is.EqualTo("No.2 Side Road"));
    }

    [Test]
    public async Task Create_address_for_missing_user_returns_not_found()
    {
        using var missingUserClient = _factory.CreateAuthenticatedClient(AccountRole.Customer, 999);
        var response = await missingUserClient.PostAsJsonAsync("/api/user/addresses", new
        {
            userId = 999,
            address = "Nowhere",
            contactName = "Ghost",
            phoneNumber = "13700000000",
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Success, Is.False);
        Assert.That(body?.Message, Is.EqualTo("用户不存在"));
    }

    [Test]
    public async Task Create_address_with_invalid_phone_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/user/addresses", new
        {
            userId = UserId,
            address = "No.3 Road",
            contactName = "Carol",
            phoneNumber = "123", // 非 11 位
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record AddressDto(int Id, int UserId, string Address, string? HouseNumber, string ContactName, string PhoneNumber);
}
