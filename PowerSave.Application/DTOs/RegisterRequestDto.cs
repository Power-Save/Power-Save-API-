namespace PowerSave.Application.DTOs;

public class RegisterRequestDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ConfirmPassword { get; set; }
    public bool ConsentToAccountCreationAndDataCollection { get; set; } = true;
    public bool ConsentToProductImprovementDataUsage { get; set; } = true;
    public string ConsentDocumentVersion { get; set; } = "v1.0";
}

