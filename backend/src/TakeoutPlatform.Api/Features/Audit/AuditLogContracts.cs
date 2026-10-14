namespace TakeoutPlatform.Api.Features.Audit;

public sealed record AuditLogResponse(
    long Id,
    string Actor,
    string Role,
    string Action,
    string TargetType,
    string TargetId,
    string Result,
    DateTime TimestampUtc,
    string CorrelationId);
