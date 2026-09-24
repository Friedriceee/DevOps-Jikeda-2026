using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.User;

/// <summary>
/// 新增收货地址的请求。对应旧项目 AddressDto。
/// 校验规则参照旧 UserAddressDB 的 DataAnnotations：
/// 地址必填且不超过 255；联系人必填不超过 100；手机号必须为 11 位数字。
/// </summary>
public sealed class CreateAddressRequest
{
    [Required(ErrorMessage = "userId 不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "userId 必须大于 0")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "address 不能为空")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "address 长度必须为 1-255 个字符")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "houseNumber 长度不能超过 50 个字符")]
    public string? HouseNumber { get; set; }

    [Required(ErrorMessage = "contactName 不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "contactName 长度必须为 1-100 个字符")]
    public string? ContactName { get; set; }

    [Required(ErrorMessage = "phoneNumber 不能为空")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "phoneNumber 必须为 11 位数字")]
    public string? PhoneNumber { get; set; }
}
