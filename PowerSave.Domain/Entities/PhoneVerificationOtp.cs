namespace PowerSave.Domain.Entities;

public class PhoneVerificationOtp
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string HashedOtp { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public bool IsSuperseded { get; set; }
    public DateTime? SupersededAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

