using Microsoft.AspNetCore.Identity;

namespace PowerSave.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<UserConsent> Consents { get; set; } = new List<UserConsent>();
    public ICollection<PhoneVerificationOtp> OtpRecords { get; set; } = new List<PhoneVerificationOtp>();
    public UserOtpRateLimit? OtpRateLimit { get; set; }
}

