using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Audit;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Admin;

public enum AdminAccountOutcome
{
    Success,
    NotFound,
    InvalidRole,
}

public sealed record AdminAccountResult(
    AdminAccountOutcome Outcome,
    AdminAccountResponse? Account,
    string? Message);

/// <summary>
/// 管理员账号管理服务：查询、启用/停用、受控角色调整。
/// 所有变更操作写审计（member3 的 IAuditLogger）。
/// 不触碰订单/价格快照，天然满足"管理员修改不改变已支付订单价格"约束。
/// </summary>
public sealed class AdminAccountService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _timeProvider;

    public AdminAccountService(AppDbContext db, IAuditLogger audit, TimeProvider timeProvider)
    {
        _db = db;
        _audit = audit;
        _timeProvider = timeProvider;
    }

    /// <summary>分页查询账号，可按角色筛选。</summary>
    public async Task<PagedResult<AdminAccountResponse>> ListAsync(
        string? role,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.Accounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<AccountRole>(role, true, out var parsedRole))
        {
            query = query.Where(a => a.Role == parsedRole);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminAccountResponse(
                a.Id, a.Username, a.Role.ToString(), a.ProfileId, a.IsActive, a.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminAccountResponse>(items, page, pageSize, total);
    }

    /// <summary>启用/停用账号。</summary>
    public async Task<AdminAccountResult> SetStatusAsync(
        int accountId,
        bool isActive,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account is null)
            return new AdminAccountResult(AdminAccountOutcome.NotFound, null, "账号不存在");

        account.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(new AuditEvent(
            actor, nameof(AccountRole.Administrator),
            isActive ? "AccountEnabled" : "AccountDisabled",
            nameof(Account), accountId.ToString(),
            "Success", _timeProvider.GetUtcNow().UtcDateTime, correlationId),
            cancellationToken);

        return new AdminAccountResult(AdminAccountOutcome.Success, ToResponse(account), null);
    }

    /// <summary>受控调整账号角色。</summary>
    public async Task<AdminAccountResult> SetRoleAsync(
        int accountId,
        string role,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AccountRole>(role, true, out var parsedRole))
            return new AdminAccountResult(AdminAccountOutcome.InvalidRole, null, "角色无效");

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account is null)
            return new AdminAccountResult(AdminAccountOutcome.NotFound, null, "账号不存在");

        account.Role = parsedRole;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(new AuditEvent(
            actor, nameof(AccountRole.Administrator),
            "AccountRoleChanged",
            nameof(Account), accountId.ToString(),
            $"Success:{parsedRole}", _timeProvider.GetUtcNow().UtcDateTime, correlationId),
            cancellationToken);

        return new AdminAccountResult(AdminAccountOutcome.Success, ToResponse(account), null);
    }

    private static AdminAccountResponse ToResponse(Account account) => new(
        account.Id, account.Username, account.Role.ToString(),
        account.ProfileId, account.IsActive, account.CreatedAtUtc);
}
