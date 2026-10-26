namespace PowerSave.Application.DTOs;

public class VerifyPhoneRequestDto
{
    public string Identifier { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

