namespace TakeoutPlatform.Api.Features.User;

public sealed record AddressResponse(
    int Id,
    int UserId,
    string Address,
    string? HouseNumber,
    string ContactName,
    string PhoneNumber);
