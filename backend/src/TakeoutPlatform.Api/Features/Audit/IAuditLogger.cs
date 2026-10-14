namespace TakeoutPlatform.Api.Features.Audit;

/// <summary>
/// 审计记录器契约。业务 Service 通过它记录敏感操作，不关心持久化细节。
///
/// Sprint 4 分工：接口与 AuditEvent 字段由 member2 + member3 在 Day 1 共同约定；
/// 真正的追加式持久化实现由 member3 提供（Features/Audit 下的 AuditLogService）。
/// member2 侧默认注册 <see cref="NullAuditLogger"/> 以便独立编译与测试，
/// member3 合并时将 DI 注册替换为真实实现即可。
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}

/// <summary>
/// 空实现：不持久化，仅保证 member2 模块可独立运行与测试。
/// member3 的真实 AuditLogService 落地后替换此注册。
/// </summary>
public sealed class NullAuditLogger : IAuditLogger
{
    public Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
