using Microsoft.Extensions.Options;
using Push.Service.Settings;

namespace Push.Service.Services;

public class FakePushSender(IOptions<PushSettings> pushSettings, ILogger<FakePushSender> logger)
    : IPushSender
{
    private readonly PushSettings _pushSettings = pushSettings.Value;

    public Task<bool> SendPushAsync(string deviceToken, string title, string message, string? platform = null, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("FAKE PUSH SENDER: Would send push notification to {DeviceToken}", deviceToken);
            logger.LogInformation("FAKE PUSH SENDER: Platform: {Platform}", platform ?? "unknown");
            logger.LogInformation("FAKE PUSH SENDER: Title: {Title}", title);
            logger.LogInformation("FAKE PUSH SENDER: Message: {Message}", message);
            
            Thread.Sleep(500);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FAKE PUSH SENDER: Error in fake push sending");
            return Task.FromResult(false);
        }
    }
}