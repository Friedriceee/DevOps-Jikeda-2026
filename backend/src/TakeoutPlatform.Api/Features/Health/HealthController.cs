using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;

namespace TakeoutPlatform.Api.Features.Health;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>健康检查，用于确认服务已启动。</summary>
    [HttpGet]
    public ActionResult<ApiResult<HealthInfo>> Get()
        => Ok(ApiResult<HealthInfo>.Ok(new HealthInfo("ok", DateTimeOffset.UtcNow)));

    public record HealthInfo(string Status, DateTimeOffset ServerTime);
}
