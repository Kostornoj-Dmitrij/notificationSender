using Email.Service.Configuration;
using Microsoft.Extensions.Options;

namespace Email.Service.Services;

public class FakeEmailSender : IEmailSender
{
    private readonly ILogger<FakeEmailSender> _logger;
    private readonly SmtpSettings _smtpSettings;

    public FakeEmailSender(IOptions<SmtpSettings> smtpSettings, ILogger<FakeEmailSender> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public Task<bool> SendEmailAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("FAKE EMAIL SENDER: Would send email to {Recipient}", recipient);
            _logger.LogInformation("FAKE EMAIL SENDER: Subject: {Subject}", subject);
            _logger.LogInformation("FAKE EMAIL SENDER: Message: {Message}", message);
            
            Thread.Sleep(500);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAKE EMAIL SENDER: Error in fake email sending");
            return Task.FromResult(false);
        }
    }
}