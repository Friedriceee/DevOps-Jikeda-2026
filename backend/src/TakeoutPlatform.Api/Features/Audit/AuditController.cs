using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Audit;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = nameof(AccountRole.Administrator))]
public sealed class AuditController : ControllerBase
{
    private readonly AuditLogService _auditLogs;

    public AuditController(AuditLogService auditLogs) => _auditLogs = auditLogs;

    [HttpGet]
    public async Task<ActionResult<ApiResult<PagedResult<AuditLogResponse>>>> Query(
        [FromQuery] string? actor,
        [FromQuery] string? action,
        [FromQuery] string? targetType,
        [FromQuery] string? result,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var logs = await _auditLogs.QueryAsync(
            actor, action, targetType, result, fromUtc, toUtc,
            page, pageSize, cancellationToken);

        return Ok(ApiResult<PagedResult<AuditLogResponse>>.Ok(logs));
    }
}
