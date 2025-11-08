using Email.Service.Settings;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;

namespace Email.Service.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> smtpSettings, ILogger<SmtpEmailSender> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", recipient));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("plain") { Text = message };

            using var client = new SmtpClient();
            
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            
            await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password, cancellationToken);
            await client.SendAsync(emailMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            return false;
        }
    }
}