namespace PowerSave.Application.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public int? RemainingWaitSeconds { get; set; }
    public DateTime? NextAllowedResendUtc { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, string? errorCode = null, int? remainingWaitSeconds = null, DateTime? nextAllowedUtc = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            RemainingWaitSeconds = remainingWaitSeconds,
            NextAllowedResendUtc = nextAllowedUtc,
            Data = default
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    public static new ApiResponse Fail(string message, string? errorCode = null, int? remainingWaitSeconds = null, DateTime? nextAllowedUtc = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            RemainingWaitSeconds = remainingWaitSeconds,
            NextAllowedResendUtc = nextAllowedUtc
        };
    }
}

