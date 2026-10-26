namespace PowerSave.Infrastructure.Sms;

public class SmsOptions
{
    public const string SectionName = "SmsProvider";

    public string Provider { get; set; } = "Dev";
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = "PowerSave";
}

