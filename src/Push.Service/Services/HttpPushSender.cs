using Microsoft.Extensions.Options;
using Push.Service.Settings;
using System.Text;
using System.Text.Json;

namespace Push.Service.Services;

public class HttpPushSender(
    IOptions<PushSettings> pushSettings,
    ILogger<HttpPushSender> logger,
    HttpClient httpClient)
    : IPushSender
{
    private readonly PushSettings _pushSettings = pushSettings.Value;

    public async Task<bool> SendPushAsync(string deviceToken, string title, string message, string? platform = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_pushSettings.TestMode)
            {
                logger.LogInformation("TEST MODE: Push would be sent: {Title} - {Message}", title, message);
                return true;
            }

            var pushMessage = new
            {
                Type = "push",
                Title = title,
                Message = message,
                Platform = platform ?? "web",
                Timestamp = DateTime.UtcNow,
                DeviceToken = deviceToken
            };

            var jsonMessage = JsonSerializer.Serialize(pushMessage);
            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            var url = $"{_pushSettings.PushTesterUrl}/api/push";

            try
            {
                var response = await httpClient.PostAsync(url, content, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Push notification sent to Push.Tester: {Title}", title);
                    return true;
                }
                else
                {
                    logger.LogWarning("Failed to send push to Push.Tester. Status: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to Push.Tester");
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push notification");
            return false;
        }
    }
}