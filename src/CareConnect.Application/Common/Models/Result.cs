namespace CareConnect.Application.Common.Models;

/// <summary>
/// Why an operation failed, so controllers can pick an HTTP status without services
/// having to know anything about HTTP.
/// </summary>
public enum ResultStatus
{
    Success = 0,
    ValidationFailed,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

public class Result<T>
{
    public bool Succeeded { get; init; }
    public ResultStatus Status { get; init; } = ResultStatus.Success;
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static Result<T> Success(T data, string message = "Request completed successfully.") =>
        new() { Succeeded = true, Status = ResultStatus.Success, Message = message, Data = data };

    public static Result<T> Failure(ResultStatus status, string message, IReadOnlyList<string>? errors = null) =>
        new() { Succeeded = false, Status = status, Message = message, Errors = errors ?? [] };

    public static Result<T> Invalid(string message, IReadOnlyList<string>? errors = null) =>
        Failure(ResultStatus.ValidationFailed, message, errors);

    public static Result<T> Unauthorized(string message) =>
        Failure(ResultStatus.Unauthorized, message);

    public static Result<T> NotFound(string message) =>
        Failure(ResultStatus.NotFound, message);

    public static Result<T> Conflict(string message) =>
        Failure(ResultStatus.Conflict, message);
}
