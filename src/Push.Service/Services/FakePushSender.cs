using Microsoft.Extensions.Options;
using Push.Service.Settings;

namespace Push.Service.Services;

public class FakePushSender : IPushSender
{
    private readonly ILogger<FakePushSender> _logger;
    private readonly PushSettings _pushSettings;

    public FakePushSender(IOptions<PushSettings> pushSettings, ILogger<FakePushSender> logger)
    {
        _pushSettings = pushSettings.Value;
        _logger = logger;
    }

    public Task<bool> SendPushAsync(string deviceToken, string title, string message, string? platform = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("FAKE PUSH SENDER: Would send push notification to {DeviceToken}", deviceToken);
            _logger.LogInformation("FAKE PUSH SENDER: Platform: {Platform}", platform ?? "unknown");
            _logger.LogInformation("FAKE PUSH SENDER: Title: {Title}", title);
            _logger.LogInformation("FAKE PUSH SENDER: Message: {Message}", message);
            
            Thread.Sleep(500);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAKE PUSH SENDER: Error in fake push sending");
            return Task.FromResult(false);
        }
    }
}