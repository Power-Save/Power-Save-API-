using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerSave.Application.Interfaces;

namespace PowerSave.Infrastructure.Sms;

public class ProductionSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly ILogger<ProductionSmsSender> _logger;

    public ProductionSmsSender(HttpClient httpClient, IOptions<SmsOptions> options, ILogger<ProductionSmsSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.ApiUrl) || string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("Production SMS provider not configured properly. Check SmsProvider configuration.");
                return false;
            }

            var payload = new
            {
                sender = _options.SenderId,
                recipient = phoneNumber.StartsWith("01") ? "+20" + phoneNumber.Substring(1) : phoneNumber,
                text = message,
                apiKey = _options.ApiKey
            };

            var response = await _httpClient.PostAsJsonAsync(_options.ApiUrl, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS dispatched successfully through provider gateway.");
                return true;
            }

            _logger.LogError("SMS gateway returned unsuccessful status code: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during external SMS delivery.");
            return false;
        }
    }
}

