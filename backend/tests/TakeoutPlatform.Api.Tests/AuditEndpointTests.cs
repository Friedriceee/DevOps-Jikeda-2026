using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

public class AuditEndpointTests
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
    public async Task Anonymous_caller_is_unauthorized()
    {
        using var client = _factory.CreateClient();
        Assert.That((await client.GetAsync("/api/admin/audit-logs")).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase(AccountRole.Customer)]
    [TestCase(AccountRole.Merchant)]
    [TestCase(AccountRole.Rider)]
    public async Task Non_administrator_is_forbidden(AccountRole role)
    {
        using var client = _factory.CreateAuthenticatedClient(role, 1);
        Assert.That((await client.GetAsync("/api/admin/audit-logs")).StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Administrator_action_is_persisted_and_queryable()
    {
        using var client = _factory.CreateAuthenticatedClient(AccountRole.Administrator, 0, 1000);
        var created = await client.PostAsJsonAsync("/api/admin/coupons", new
        {
            code = "AUDIT10", name = "Audited coupon", minimumSpend = 20m, discountAmount = 10m,
            startsAtUtc = DateTimeOffset.UtcNow.AddDays(-1), expiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            isActive = true,
        });
        Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var response = await client.GetAsync("/api/admin/audit-logs?action=CouponCreated");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedDto<AuditDto>>>();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.Data?.TotalCount, Is.EqualTo(1));
            Assert.That(body?.Data?.Items[0].Actor, Is.EqualTo("test-administrator"));
            Assert.That(body?.Data?.Items[0].CorrelationId, Is.Not.Empty);
        }));
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record PagedDto<T>(List<T> Items, int Page, int PageSize, int TotalCount);
    private sealed record AuditDto(long Id, string Actor, string Role, string Action, string TargetType,
        string TargetId, string Result, DateTime TimestampUtc, string CorrelationId);
}
