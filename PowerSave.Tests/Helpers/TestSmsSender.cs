using PowerSave.Application.Interfaces;

namespace PowerSave.Tests.Helpers;

public class TestSmsSender : ISmsSender
{
    public List<(string PhoneNumber, string Message)> SentMessages { get; } = new();
    public bool ShouldFail { get; set; } = false;

    public string? LastExtractedOtp
    {
        get
        {
            var last = SentMessages.LastOrDefault();
            if (last == default) return null;

            var match = System.Text.RegularExpressions.Regex.Match(last.Message, @"\b\d{6}\b");
            return match.Success ? match.Value : null;
        }
    }

    public Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((phoneNumber, message));
        if (ShouldFail)
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
}

