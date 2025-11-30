using Microsoft.Extensions.Options;
using Push.Service.Settings;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Push.Service.Services;

public class WebSocketPushSender : IPushSender
{
    private readonly PushSettings _pushSettings;
    private readonly ILogger<WebSocketPushSender> _logger;
    private readonly HttpClient _httpClient;

    public WebSocketPushSender(
        IOptions<PushSettings> pushSettings, 
        ILogger<WebSocketPushSender> logger,
        HttpClient httpClient)
    {
        _pushSettings = pushSettings.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> SendPushAsync(string deviceToken, string title, string message, string? platform = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_pushSettings.TestMode)
            {
                _logger.LogInformation("TEST MODE: Push would be sent: {Title} - {Message}", title, message);
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

            try
            {
                var response = await _httpClient.PostAsync("http://push.tester:8080/api/push", content, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Push notification sent to Push.Tester: {Title}", title);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send push to Push.Tester. Status: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Push.Tester");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification");
            return false;
        }
    }
}