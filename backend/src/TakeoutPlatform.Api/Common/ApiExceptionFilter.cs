using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TakeoutPlatform.Api.Common;

/// <summary>
/// 兜底异常处理：未捕获的异常统一转成 500 + ApiResult 结构，避免把堆栈直接抛给前端。
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) => _logger = logger;

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "未处理异常: {Path}", context.HttpContext.Request.Path);

        context.Result = new ObjectResult(ApiResult.Fail("服务器内部错误"))
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };
        context.ExceptionHandled = true;
    }
}
