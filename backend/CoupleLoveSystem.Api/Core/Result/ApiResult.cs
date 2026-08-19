namespace CoupleLoveSystem.Core.Result;

/// <summary>统一返回模型。Data 为业务载荷，Success/Code/Msg 描述状态。</summary>
public class ApiResult<T>
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public T? Data { get; set; }
    public bool Success { get; set; }

    public static ApiResult<T> Ok(T data, string msg = "成功") =>
        new() { Code = 200, Msg = msg, Data = data, Success = true };

    public static ApiResult<T> Fail(int code, string msg) =>
        new() { Code = code, Msg = msg, Data = default, Success = false };
}

/// <summary>无 Data 场景复用 ApiResult&lt;object&gt; 便捷方法。</summary>
public static class ApiResults
{
    public static ApiResult<object> Ok(object data, string msg = "成功") => ApiResult<object>.Ok(data, msg);
    public static ApiResult<object> Fail(int code, string msg) => ApiResult<object>.Fail(code, msg);
}

/// <summary>错误码（与文档 §1.2 对齐）。</summary>
public static class ErrorCode
{
    public const int Success = 200;
    public const int ParamInvalid = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int PayloadTooLarge = 413;
    public const int UnsupportedMedia = 415;
    public const int TooManyRequests = 429;
    public const int ServerError = 500;
}

/// <summary>权限/未找到等业务异常，由全局中间件转成对应 HTTP 状态码。</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string msg) : base(msg) { }
}
public class NotFoundException : Exception
{
    public NotFoundException(string msg) : base(msg) { }
}
public class ConflictException : Exception
{
    public ConflictException(string msg) : base(msg) { }
}
/// <summary>请求频率超限（登录防爆破等），由全局中间件转成 429。</summary>
public class RateLimitedException : Exception
{
    public RateLimitedException(string msg) : base(msg) { }
}
