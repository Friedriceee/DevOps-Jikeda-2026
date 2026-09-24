namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// 订单状态。把旧项目里隐式的 int 魔数显式化。
/// 数值与旧项目 OrderDB.State 保持一致，便于数据兼容：
/// 0=待支付, 1=已付款(骑手未接单), 2=派送中, 3=已送达。
/// 本次功能只用到 Pending（创建即待支付）与取消，其余状态先保留定义不实现流转。
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Delivering = 2,
    Delivered = 3,
}
