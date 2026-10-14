using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Features.Audit;

public sealed class AuditLogService : IAuditLogger
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db) => _db = db;

    public async Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        _db.AuditLogs.Add(new AuditLog
        {
            Actor = Required(auditEvent.Actor, 100, nameof(auditEvent.Actor)),
            Role = Required(auditEvent.Role, 30, nameof(auditEvent.Role)),
            Action = Required(auditEvent.Action, 80, nameof(auditEvent.Action)),
            TargetType = Required(auditEvent.TargetType, 80, nameof(auditEvent.TargetType)),
            TargetId = Required(auditEvent.TargetId, 100, nameof(auditEvent.TargetId)),
            Result = Required(auditEvent.Result, 200, nameof(auditEvent.Result)),
            TimestampUtc = NormalizeUtc(auditEvent.TimestampUtc),
            CorrelationId = Required(auditEvent.CorrelationId, 100, nameof(auditEvent.CorrelationId)),
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogResponse>> QueryAsync(
        string? actor,
        string? action,
        string? targetType,
        string? result,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(actor))
            query = query.Where(log => log.Actor.Contains(actor.Trim()));
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(log => log.Action == action.Trim());
        if (!string.IsNullOrWhiteSpace(targetType))
            query = query.Where(log => log.TargetType == targetType.Trim());
        if (!string.IsNullOrWhiteSpace(result))
            query = query.Where(log => log.Result.Contains(result.Trim()));
        if (fromUtc.HasValue)
            query = query.Where(log => log.TimestampUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(log => log.TimestampUtc <= toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.TimestampUtc)
            .ThenByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AuditLogResponse(
                log.Id,
                log.Actor,
                log.Role,
                log.Action,
                log.TargetType,
                log.TargetId,
                log.Result,
                log.TimestampUtc,
                log.CorrelationId))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogResponse>(items, page, pageSize, total);
    }

    private static string Required(string value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required", field);

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
