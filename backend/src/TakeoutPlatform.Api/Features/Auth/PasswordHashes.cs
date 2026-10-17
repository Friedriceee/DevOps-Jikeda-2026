namespace TakeoutPlatform.Api.Features.Auth;

public static class PasswordHashes
{
    /// <summary>
    /// A well-formed Base64 value that is not a valid ASP.NET Core Identity password hash
    /// (its format marker byte is 0x02). <c>PasswordHasher.VerifyHashedPassword</c> returns
    /// <c>Failed</c> for it without throwing, so an account carrying this value can never sign in
    /// until a real password is set.
    /// </summary>
    public const string Unusable = "Ag==";
}
