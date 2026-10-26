namespace PowerSave.Application.DTOs;

public class ResendOtpResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RemainingResendsInBatch { get; set; }
    public DateTime? NextAllowedResendUtc { get; set; }
    public int? RemainingWaitSeconds { get; set; }
}

