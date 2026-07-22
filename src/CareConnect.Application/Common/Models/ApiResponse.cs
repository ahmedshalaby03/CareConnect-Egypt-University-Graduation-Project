namespace CareConnect.Application.Common.Models;

/// <summary>
/// The single envelope every endpoint returns, so the Angular client only ever has to
/// unwrap one shape.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T? data, string message = "Request completed successfully.") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

/// <summary>Non-generic convenience wrapper for endpoints with no payload.</summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message) =>
        new() { Success = true, Message = message };

    public static new ApiResponse Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
