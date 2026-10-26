namespace PowerSave.Application.DTOs;

public class VerifyPhoneResponseDto
{
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
}

