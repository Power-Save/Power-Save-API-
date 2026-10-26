namespace PowerSave.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PowerSave.API";
    public string Audience { get; set; } = "PowerSave.App";
    public int ExpirationMinutes { get; set; } = 60;
}

