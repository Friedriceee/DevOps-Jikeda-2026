using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Admin;

/// <summary>管理员视角的账号摘要。</summary>
public sealed record AdminAccountResponse(
    int Id,
    string Username,
    string Role,
    int ProfileId,
    bool IsActive,
    DateTime CreatedAtUtc);

/// <summary>启用/停用账号请求。</summary>
public sealed class UpdateAccountStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}

/// <summary>受控调整账号角色请求。</summary>
public sealed class UpdateAccountRoleRequest
{
    [Required(ErrorMessage = "role is required")]
    public string Role { get; set; } = string.Empty;
}
