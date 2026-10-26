using PowerSave.Application.Common.Localization;

namespace PowerSave.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public AuthErrorCode ErrorCode { get; init; } = AuthErrorCode.None;
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; } = 200;
    public int? RemainingWaitSeconds { get; init; }
    public DateTime? NextAllowedResendUtc { get; init; }

    public static Result<T> Success(T data, int statusCode = 200, string message = "")
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data,
            StatusCode = statusCode,
            Message = message
        };
    }

    public static Result<T> Failure(AuthErrorCode code, int statusCode = 400, string? customMessage = null, params object[] args)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Data = default,
            ErrorCode = code,
            StatusCode = statusCode,
            Message = customMessage ?? AuthMessages.GetArabicMessage(code, args)
        };
    }

    public static Result<T> RateLimited(AuthErrorCode code, int remainingSeconds, DateTime nextAllowedUtc, string? customMessage = null)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Data = default,
            ErrorCode = code,
            StatusCode = 429,
            RemainingWaitSeconds = remainingSeconds,
            NextAllowedResendUtc = nextAllowedUtc,
            Message = customMessage ?? AuthMessages.GetArabicMessage(code, remainingSeconds)
        };
    }
}

public class Result : Result<bool>
{
    public static Result Success(int statusCode = 200, string message = "")
    {
        return new Result
        {
            IsSuccess = true,
            Data = true,
            StatusCode = statusCode,
            Message = message
        };
    }

    public static new Result Failure(AuthErrorCode code, int statusCode = 400, string? customMessage = null, params object[] args)
    {
        return new Result
        {
            IsSuccess = false,
            Data = false,
            ErrorCode = code,
            StatusCode = statusCode,
            Message = customMessage ?? AuthMessages.GetArabicMessage(code, args)
        };
    }

    public static new Result RateLimited(AuthErrorCode code, int remainingSeconds, DateTime nextAllowedUtc, string? customMessage = null)
    {
        return new Result
        {
            IsSuccess = false,
            Data = false,
            ErrorCode = code,
            StatusCode = 429,
            RemainingWaitSeconds = remainingSeconds,
            NextAllowedResendUtc = nextAllowedUtc,
            Message = customMessage ?? AuthMessages.GetArabicMessage(code, remainingSeconds)
        };
    }
}

