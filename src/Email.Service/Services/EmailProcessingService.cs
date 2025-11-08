using Common.DTO;
using Email.Service.Data;
using Email.Service.Models;
using Email.Service.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Email.Service.Services;

public class EmailProcessingService
{
    private readonly ILogger<EmailProcessingService> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IServiceProvider _serviceProvider;
    private readonly RetrySettings _retrySettings;

    public EmailProcessingService(
        ILogger<EmailProcessingService> logger,
        IEmailSender emailSender,
        IServiceProvider serviceProvider,
        IOptions<RetrySettings> retrySettings)
    {
        _logger = logger;
        _emailSender = emailSender;
        _serviceProvider = serviceProvider;
        _retrySettings = retrySettings.Value;
    }

    public async Task ProcessNotificationAsync(NotificationRequest notification)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmailDbContext>();

        _logger.LogInformation("Processing email notification: {NotificationId}", notification.Id);

        try
        {
            var existingNotification = await dbContext.EmailNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == notification.Id);

            if (existingNotification != null)
            {
                _logger.LogWarning("Notification {NotificationId} already processed", notification.Id);
                return;
            }

            var emailNotification = new EmailNotification
            {
                NotificationId = notification.Id,
                Recipient = notification.Recipient,
                Subject = notification.Subject,
                Message = notification.Message,
                Status = "pending",
                RetryCount = 0
            };

            dbContext.EmailNotifications.Add(emailNotification);
            await dbContext.SaveChangesAsync();

            var success = await SendWithRetryAsync(notification, emailNotification, dbContext);

            emailNotification.Status = success ? "sent" : "failed";
            emailNotification.SentAt = success ? DateTime.UtcNow : null;

            await dbContext.SaveChangesAsync();

            if (success)
            {
                _logger.LogInformation("Email sent successfully to {Recipient} for notification {NotificationId}",
                    notification.Recipient, notification.Id);
            }
            else
            {
                _logger.LogError("Failed to send email to {Recipient} for notification {NotificationId} after retries",
                    notification.Recipient, notification.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email notification {NotificationId}", notification.Id);
            throw;
        }
    }

    private async Task<bool> SendWithRetryAsync(
        NotificationRequest notification,
        EmailNotification emailNotification,
        EmailDbContext dbContext)
    {
        for (int attempt = 0; attempt < _retrySettings.MaxRetries; attempt++)
        {
            try
            {
                var success = await _emailSender.SendEmailAsync(
                    notification.Recipient,
                    notification.Subject,
                    notification.Message);

                if (success)
                {
                    return true;
                }

                _logger.LogWarning("Email sending failed for {Recipient} (attempt {Attempt})",
                    notification.Recipient, attempt + 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email sending error for {Recipient} (attempt {Attempt})",
                    notification.Recipient, attempt + 1);
            }

            emailNotification.RetryCount = attempt + 1;
            await dbContext.SaveChangesAsync();

            if (attempt < _retrySettings.MaxRetries - 1)
            {
                var delay = TimeSpan.FromSeconds(_retrySettings.RetryDelaysInSeconds[attempt]);
                await Task.Delay(delay);
            }
        }

        return false;
    }
}