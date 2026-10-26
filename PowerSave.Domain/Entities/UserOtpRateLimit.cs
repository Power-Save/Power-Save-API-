namespace PowerSave.Domain.Entities;

public class UserOtpRateLimit
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public DateTime LastOtpSentAtUtc { get; set; }
    public int ResendAttemptsInCurrentBatch { get; set; }
    public int BatchIndex { get; set; } = 1;
    public DateTime? CooldownUntilUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

