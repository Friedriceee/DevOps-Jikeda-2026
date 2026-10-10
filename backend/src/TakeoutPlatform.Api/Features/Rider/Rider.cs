namespace TakeoutPlatform.Api.Features.Rider;

/// <summary>
/// 骑手实体。承载责任链筛选所需的资格数据：
/// 是否在职(IsActive)、配送区域(Zone)、当前是否接受派单(IsAvailable)、
/// 并发接单上限(Capacity) 与当前在途单量(ActiveDeliveryCount)。
///
/// 主键 Id 由数据库自增生成；骑手账号 Account.ProfileId 指向本实体 Id。
/// </summary>
public class Rider
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>是否在职。离职/停用骑手不参与派单。</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>配送区域代码，责任链据此匹配订单所在区域。</summary>
    public string Zone { get; set; } = string.Empty;

    /// <summary>骑手是否愿意接单（上线/下线开关）。</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>并发接单上限。</summary>
    public int Capacity { get; set; } = 3;

    /// <summary>当前在途（尚未送达）单量。接单 +1，送达 -1。</summary>
    public int ActiveDeliveryCount { get; set; }

    public ICollection<RiderDelivery> Deliveries { get; set; } = new List<RiderDelivery>();
}
