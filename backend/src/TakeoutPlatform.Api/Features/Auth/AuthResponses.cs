namespace TakeoutPlatform.Api.Features.Auth;

public sealed record CustomerRegistrationResponse(
    int AccountId, int CustomerId, string Username, string DisplayName);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    int AccountId,
    string Role,
    int ProfileId);
