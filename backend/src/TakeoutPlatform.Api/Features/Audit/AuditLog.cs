namespace TakeoutPlatform.Api.Features.Audit;

/// <summary>
/// Immutable business and security audit record. Audit logs are append-only:
/// the application exposes create-through-IAuditLogger and read-only query APIs.
/// </summary>
public sealed class AuditLog
{
    public long Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
