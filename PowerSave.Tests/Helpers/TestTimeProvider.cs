using PowerSave.Application.Common.Interfaces;

namespace PowerSave.Tests.Helpers;

public class TestTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    public void AdvanceSeconds(int seconds) => UtcNow = UtcNow.AddSeconds(seconds);
    public void AdvanceMinutes(int minutes) => UtcNow = UtcNow.AddMinutes(minutes);
    public void AdvanceHours(int hours) => UtcNow = UtcNow.AddHours(hours);
    public void AdvanceDays(int days) => UtcNow = UtcNow.AddDays(days);
}

