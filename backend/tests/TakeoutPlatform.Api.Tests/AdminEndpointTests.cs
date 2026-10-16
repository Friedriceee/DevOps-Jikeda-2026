using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// 管理员后端接口集成测试（完整 HTTP 管线 + JWT 鉴权）。
/// 覆盖授权矩阵（匿名 401、非管理员 403、管理员正常）以及优惠券/账号/订单的 happy path。
/// </summary>
public class AdminEndpointTests
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

    // ---------- 授权矩阵 ----------

    [Test]
    public async Task Anonymous_request_to_admin_endpoint_returns_401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/accounts");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase(AccountRole.Customer)]
    [TestCase(AccountRole.Merchant)]
    [TestCase(AccountRole.Rider)]
    public async Task Non_admin_request_to_admin_endpoint_returns_403(AccountRole role)
    {
        using var client = _factory.CreateAuthenticatedClient(role, 1);
        var response = await client.GetAsync("/api/admin/accounts");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Administrator_can_list_accounts()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Administrator, 0);
        var response = await client.GetAsync("/api/admin/accounts");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedDto<AccountDto>>>();
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data, Is.Not.Null);
    }

    // ---------- 优惠券 ----------

    [Test]
    public async Task Administrator_can_create_and_list_coupons()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Administrator, 0);

        var create = await client.PostAsJsonAsync("/api/admin/coupons", new
        {
            code = "ENDPT10",
            name = "Endpoint coupon",
            minimumSpend = 20m,
            discountAmount = 10m,
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            expiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            isActive = true,
        });
        Assert.That(create.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var list = await client.GetAsync("/api/admin/coupons");
        var body = await list.Content.ReadFromJsonAsync<ApiResponse<List<CouponDto>>>();
        Assert.That(body?.Data?.Any(c => c.Code == "ENDPT10"), Is.True);
    }

    [Test]
    public async Task Administrator_can_issue_coupon_quantity_to_a_customer()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Administrator, 0);

        var issue = await client.PostAsJsonAsync("/api/admin/coupons/1/issue", new
        {
            userId = 1,
            quantity = 3,
        });
        Assert.That(issue.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var list = await client.GetAsync("/api/admin/coupon-allocations");
        var body = await list.Content.ReadFromJsonAsync<ApiResponse<List<CouponAllocationDto>>>();
        var allocation = body?.Data?.Single(item => item.UserId == 1 && item.CouponId == 1);
        Assert.That(allocation?.IssuedQuantity, Is.EqualTo(23));
        Assert.That(allocation?.RemainingQuantity, Is.EqualTo(23));
    }

    [Test]
    public async Task Create_coupon_with_non_positive_discount_returns_bad_request()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Administrator, 0);
        var response = await client.PostAsJsonAsync("/api/admin/coupons", new
        {
            code = "BADAMT",
            name = "Bad amount",
            minimumSpend = 20m,
            discountAmount = 0m, // invalid
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            expiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            isActive = true,
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_coupon_with_invalid_date_range_returns_bad_request()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Administrator, 0);
        var response = await client.PostAsJsonAsync("/api/admin/coupons", new
        {
            code = "BADRANGE",
            name = "Bad range",
            minimumSpend = 20m,
            discountAmount = 10m,
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            expiresAtUtc = DateTimeOffset.UtcNow.AddDays(5), // expire before start
            isActive = true,
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ---------- 订单查询 ----------

    [Test]
    public async Task Administrator_can_query_orders()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Administrator, 0);
        var response = await client.GetAsync("/api/admin/orders?page=1&pageSize=10");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedDto<AdminOrderDto>>>();
        Assert.That(body?.Success, Is.True);
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record PagedDto<T>(List<T> Items, int Page, int PageSize, int TotalCount);
    private sealed record AccountDto(int Id, string Username, string Role, int ProfileId, bool IsActive);
    private sealed record CouponDto(int Id, string Code, string Name, decimal MinimumSpend, decimal DiscountAmount, bool IsActive);
    private sealed record AdminOrderDto(int OrderId, int UserId, string Status, decimal Price);
    private sealed record CouponAllocationDto(
        int Id, int UserId, int CouponId, int IssuedQuantity, int UsedQuantity, int RemainingQuantity);
}
