namespace TakeoutPlatform.Api.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "TakeoutPlatform";
    public string Audience { get; set; } = "TakeoutPlatform.Client";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
