using Common.DTO;
using Sms.Service.Data;
using Sms.Service.Models;
using Sms.Service.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Sms.Service.Services;

public class SmsProcessingService
{
    private readonly ILogger<SmsProcessingService> _logger;
    private readonly ISmsSender _smsSender;
    private readonly IServiceProvider _serviceProvider;
    private readonly RetrySettings _retrySettings;

    public SmsProcessingService(
        ILogger<SmsProcessingService> logger,
        ISmsSender smsSender,
        IServiceProvider serviceProvider,
        IOptions<RetrySettings> retrySettings)
    {
        _logger = logger;
        _smsSender = smsSender;
        _serviceProvider = serviceProvider;
        _retrySettings = retrySettings.Value;
    }

    public async Task ProcessNotificationAsync(NotificationRequest notification)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SmsDbContext>();

        _logger.LogInformation("Processing SMS notification: {NotificationId}", notification.Id);

        try
        {
            var existingNotification = await dbContext.SmsNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == notification.Id);

            if (existingNotification != null)
            {
                _logger.LogWarning("Notification {NotificationId} already processed", notification.Id);
                return;
            }

            var smsNotification = new SmsNotification
            {
                NotificationId = notification.Id,
                PhoneNumber = notification.Recipient,
                Message = notification.Message,
                Status = "pending",
                RetryCount = 0
            };

            dbContext.SmsNotifications.Add(smsNotification);
            await dbContext.SaveChangesAsync();

            var success = await SendWithRetryAsync(notification, smsNotification, dbContext);

            smsNotification.Status = success ? "sent" : "failed";
            smsNotification.SentAt = success ? DateTime.UtcNow : null;

            await dbContext.SaveChangesAsync();

            if (success)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber} for notification {NotificationId}",
                    notification.Recipient, notification.Id);
            }
            else
            {
                _logger.LogError("Failed to send SMS to {PhoneNumber} for notification {NotificationId} after retries",
                    notification.Recipient, notification.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SMS notification {NotificationId}", notification.Id);
            throw;
        }
    }

    private async Task<bool> SendWithRetryAsync(
        NotificationRequest notification,
        SmsNotification smsNotification,
        SmsDbContext dbContext)
    {
        for (int attempt = 0; attempt < _retrySettings.MaxRetries; attempt++)
        {
            try
            {
                var success = await _smsSender.SendSmsAsync(
                    notification.Recipient,
                    notification.Message);

                if (success)
                {
                    return true;
                }

                _logger.LogWarning("SMS sending failed for {PhoneNumber} (attempt {Attempt})",
                    notification.Recipient, attempt + 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMS sending error for {PhoneNumber} (attempt {Attempt})",
                    notification.Recipient, attempt + 1);
            }

            smsNotification.RetryCount = attempt + 1;
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