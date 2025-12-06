using Common.DTO;
using Push.Service.Data;
using Push.Service.Models;
using Push.Service.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Push.Service.Services;

public class PushProcessingService(
    ILogger<PushProcessingService> logger,
    IPushSender pushSender,
    IServiceProvider serviceProvider,
    IOptions<RetrySettings> retrySettings)
{
    private readonly RetrySettings _retrySettings = retrySettings.Value;

    public async Task ProcessNotificationAsync(NotificationRequest notification)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PushDbContext>();

        logger.LogInformation("Processing push notification: {NotificationId}", notification.Id);

        try
        {
            var existingNotification = await dbContext.PushNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == notification.Id);

            if (existingNotification != null)
            {
                logger.LogWarning("Notification {NotificationId} already processed", notification.Id);
                return;
            }

            notification.Metadata.TryGetValue("platform", out var platform);

            var pushNotification = new PushNotification
            {
                NotificationId = notification.Id,
                DeviceToken = notification.Recipient,
                Title = notification.Subject,
                Message = notification.Message,
                Platform = platform,
                Status = "pending",
                RetryCount = 0
            };

            dbContext.PushNotifications.Add(pushNotification);
            await dbContext.SaveChangesAsync();

            var success = await SendWithRetryAsync(notification, pushNotification, dbContext);

            pushNotification.Status = success ? "sent" : "failed";
            pushNotification.SentAt = success ? DateTime.UtcNow : null;

            await dbContext.SaveChangesAsync();

            if (success)
            {
                logger.LogInformation("Push sent successfully to {DeviceToken} for notification {NotificationId}",
                    notification.Recipient, notification.Id);
            }
            else
            {
                logger.LogError("Failed to send push to {DeviceToken} for notification {NotificationId} after retries",
                    notification.Recipient, notification.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing push notification {NotificationId}", notification.Id);
            throw;
        }
    }

    private async Task<bool> SendWithRetryAsync(
        NotificationRequest notification,
        PushNotification pushNotification,
        PushDbContext dbContext)
    {
        for (int attempt = 0; attempt < _retrySettings.MaxRetries; attempt++)
        {
            try
            {
                notification.Metadata.TryGetValue("platform", out var platform);

                var success = await pushSender.SendPushAsync(
                    notification.Recipient,
                    notification.Subject,
                    notification.Message,
                    platform);

                if (success)
                {
                    return true;
                }

                logger.LogWarning("Push sending failed for {DeviceToken} (attempt {Attempt})",
                    notification.Recipient, attempt + 1);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Push sending error for {DeviceToken} (attempt {Attempt})",
                    notification.Recipient, attempt + 1);
            }

            pushNotification.RetryCount = attempt + 1;
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