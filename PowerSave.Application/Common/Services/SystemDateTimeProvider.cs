using PowerSave.Application.Common.Interfaces;

namespace PowerSave.Application.Common.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

