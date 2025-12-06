using Email.Service.Settings;
using Microsoft.Extensions.Options;

namespace Email.Service.Services;

public class FakeEmailSender(IOptions<SmtpSettings> smtpSettings, ILogger<FakeEmailSender> logger)
    : IEmailSender
{
    private readonly SmtpSettings _smtpSettings = smtpSettings.Value;

    public Task<bool> SendEmailAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("FAKE EMAIL SENDER: Would send email to {Recipient}", recipient);
            logger.LogInformation("FAKE EMAIL SENDER: Subject: {Subject}", subject);
            logger.LogInformation("FAKE EMAIL SENDER: Message: {Message}", message);
            
            Thread.Sleep(500);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FAKE EMAIL SENDER: Error in fake email sending");
            return Task.FromResult(false);
        }
    }
}