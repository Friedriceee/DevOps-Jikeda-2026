namespace TakeoutPlatform.Api.Features.Audit;

/// <summary>
/// 审计事件契约（Sprint 4，Day 1 由 member2 + member3 共同约定）。
/// 字段对应计划要求：actor、role、action、target、result、timestamp、correlationId。
/// 由各业务 Service 在敏感/关键操作后产生，member3 负责持久化为追加式 AuditLog。
/// </summary>
public sealed record AuditEvent(
    string Actor,          // 操作者标识（如管理员用户名或账号 Id）
    string Role,           // 操作者角色（Administrator/Customer/...）
    string Action,         // 动作名（如 AccountDisabled、CouponCreated）
    string TargetType,     // 目标类型（如 Account、PlatformCoupon、Order）
    string TargetId,       // 目标标识
    string Result,         // 结果（Success/Failure/...）
    DateTime TimestampUtc,
    string CorrelationId); // 关联 Id，用于串联一次请求内的多条事件
