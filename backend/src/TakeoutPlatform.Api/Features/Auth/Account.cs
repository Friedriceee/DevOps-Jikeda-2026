namespace TakeoutPlatform.Api.Features.Auth;

public sealed class Account
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AccountRole Role { get; set; }
    public int ProfileId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>账号是否启用。停用账号由管理员控制，停用后应拒绝登录（Sprint 4 member2）。</summary>
    public bool IsActive { get; set; } = true;
}
