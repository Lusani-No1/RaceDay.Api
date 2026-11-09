namespace RaceDay.Mvc.Services;

/// <summary>Result of an API call. Non-success responses are returned, not thrown.</summary>
public record ApiResult(bool Success, int StatusCode, string? Error)
{
    public static ApiResult Ok(int statusCode = 200) => new(true, statusCode, null);
    public static ApiResult Fail(int statusCode, string error) => new(false, statusCode, error);
}

public record ApiResult<T>(bool Success, int StatusCode, string? Error, T? Value)
{
    public static ApiResult<T> Ok(T value, int statusCode = 200) => new(true, statusCode, null, value);
    public static ApiResult<T> Fail(int statusCode, string error) => new(false, statusCode, error, default);
}
