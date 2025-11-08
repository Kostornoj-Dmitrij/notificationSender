using Microsoft.Extensions.Options;
using Sms.Service.Settings;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Sms.Service.Services;

public class TwilioSmsSender : ISmsSender
{
    private readonly SmsSettings _smsSettings;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(IOptions<SmsSettings> smsSettings, ILogger<TwilioSmsSender> logger)
    {
        _smsSettings = smsSettings.Value;
        _logger = logger;
        
        if (!_smsSettings.TestMode)
        {
            TwilioClient.Init(_smsSettings.AccountSid, _smsSettings.AuthToken);
        }
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_smsSettings.TestMode)
            {
                _logger.LogInformation("TEST MODE: SMS would be sent to {PhoneNumber}", phoneNumber);
                return true;
            }

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_smsSettings.FromPhoneNumber),
                to: new PhoneNumber(phoneNumber)
            );

            _logger.LogInformation("SMS sent successfully to {PhoneNumber}, SID: {MessageSid}", 
                phoneNumber, messageResource.Sid);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}