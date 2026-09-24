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
}
