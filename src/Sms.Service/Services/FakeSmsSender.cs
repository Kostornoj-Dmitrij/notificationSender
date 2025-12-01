using Microsoft.Extensions.Options;
using Sms.Service.Settings;

namespace Sms.Service.Services;

public class FakeSmsSender : ISmsSender
{
    private readonly ILogger<FakeSmsSender> _logger;
    private readonly SmsSettings _smsSettings;

    public FakeSmsSender(IOptions<SmsSettings> smsSettings, ILogger<FakeSmsSender> logger)
    {
        _smsSettings = smsSettings.Value;
        _logger = logger;
    }

    public Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("FAKE SMS SENDER: Would send SMS to {PhoneNumber}", phoneNumber);
            _logger.LogInformation("FAKE SMS SENDER: Message: {Message}", message);

            Thread.Sleep(500);

            return Task.FromResult(new SmsSendResult
            {
                Success = true,
                ExternalId = Guid.NewGuid()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAKE SMS SENDER: Error in fake SMS sending");
            return Task.FromResult(new SmsSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}