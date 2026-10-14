namespace TakeoutPlatform.Api.Common;

/// <summary>
/// 通用分页结果。用于管理员账号/订单等列表查询（Sprint 4）。
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
