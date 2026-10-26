using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PowerSave.Application.Interfaces;

namespace PowerSave.Infrastructure.Sms;

public class DevSmsSender : ISmsSender
{
    private readonly ILogger<DevSmsSender> _logger;

    public static ConcurrentBag<(string PhoneNumber, string Message, DateTime TimestampUtc)> SentMessages { get; } = new();

    public DevSmsSender(ILogger<DevSmsSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((phoneNumber, message, DateTime.UtcNow));

#if DEBUG
        _logger.LogInformation("[DEV SMS SIMULATOR] SMS dispatched to {PhoneNumber}: {Message}", phoneNumber, message);
#else
        _logger.LogInformation("[DEV SMS SIMULATOR] SMS dispatched to recipient.");
#endif

        return Task.FromResult(true);
    }
}

