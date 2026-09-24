using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Auth;

public sealed class RegisterCustomerRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{8,11}$")]
    public string PhoneNumber { get; set; } = string.Empty;
}
