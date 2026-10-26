namespace PowerSave.Application.DTOs;

public class RegisterResponseDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsPhoneVerified { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? NextResendAllowedUtc { get; set; }
    public int? RemainingWaitSeconds { get; set; }
}

