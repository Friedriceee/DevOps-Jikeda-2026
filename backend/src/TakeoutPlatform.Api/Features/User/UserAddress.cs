namespace TakeoutPlatform.Api.Features.User;

/// <summary>
/// 用户收货地址实体。对应旧项目 UserAddressDB。
/// 字段：所属用户、详细地址、门牌号、联系人、联系电话。
/// 主键 Id 由数据库自增生成。
/// </summary>
public class UserAddress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? HouseNumber { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
