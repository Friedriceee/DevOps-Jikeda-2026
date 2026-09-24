namespace TakeoutPlatform.Api.Features.User;

/// <summary>
/// 用户实体（最小化版本）。
/// 对应旧项目 UserDB，这里只保留地址与订单功能所需的字段：
/// 主键、用户名、手机号、钱包余额。
/// 主键 Id 由数据库自增生成，不再像旧项目那样手动发号。
/// </summary>
public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public decimal Wallet { get; set; }

    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
}
