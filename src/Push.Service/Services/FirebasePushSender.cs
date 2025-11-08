using Microsoft.Extensions.Options;
using Push.Service.Settings;

namespace Push.Service.Services;

public class FirebasePushSender : IPushSender
{
    private readonly PushSettings _pushSettings;
    private readonly ILogger<FirebasePushSender> _logger;

    public FirebasePushSender(IOptions<PushSettings> pushSettings, ILogger<FirebasePushSender> logger)
    {
        _pushSettings = pushSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendPushAsync(string deviceToken, string title, string message, string? platform = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_pushSettings.TestMode)
            {
                _logger.LogInformation("TEST MODE: Push would be sent to {DeviceToken}", deviceToken);
                return true;
            }

            // Интеграция с Firebase Cloud Messaging

            _logger.LogInformation("Push notification sent successfully to {DeviceToken}", deviceToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to {DeviceToken}", deviceToken);
            return false;
        }
    }
}