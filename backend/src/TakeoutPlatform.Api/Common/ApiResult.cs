namespace TakeoutPlatform.Api.Common;

/// <summary>
/// 统一响应包装。所有接口返回这个结构。
/// </summary>
public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? Code { get; set; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResult<T> Fail(string message, string? code = null) =>
        new() { Success = false, Message = message, Code = code };
}

/// <summary>无数据体时用这个。</summary>
public static class ApiResult
{
    public static ApiResult<object> Ok() => new() { Success = true };
    public static ApiResult<object> Fail(string message, string? code = null) =>
        new() { Success = false, Message = message, Code = code };
}
