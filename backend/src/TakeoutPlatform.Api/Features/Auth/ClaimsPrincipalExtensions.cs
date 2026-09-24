using System.Security.Claims;

namespace TakeoutPlatform.Api.Features.Auth;

public static class ClaimsPrincipalExtensions
{
    public const string ProfileIdClaim = "profile_id";

    public static int GetProfileId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ProfileIdClaim);
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Token does not contain a valid profile_id claim.");
    }
}
