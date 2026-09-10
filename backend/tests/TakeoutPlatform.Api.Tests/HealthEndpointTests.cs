using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// 骨架自检：确认 Web 主机能起、路由和统一响应格式生效。
/// 「商家创建菜品」的测试参照这个文件新建（建议按 SQLite in-memory 换掉 DbContext）。
/// </summary>
public class HealthEndpointTests
{
    [Test]
    public async Task Health_returns_ok_wrapped_in_api_result()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<ApiResultDto>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Success, Is.True);
    }

    private record ApiResultDto(bool Success, string? Message);
}
