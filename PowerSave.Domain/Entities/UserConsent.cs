using PowerSave.Domain.Enums;

namespace PowerSave.Domain.Entities;

public class UserConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public ConsentType ConsentType { get; set; }
    public bool IsAccepted { get; set; }
    public string DocumentVersion { get; set; } = string.Empty;
    public string PolicyIdentifier { get; set; } = string.Empty;
    public DateTime AcceptedAtUtc { get; set; }
    public string? ClientIpAddress { get; set; }
}

