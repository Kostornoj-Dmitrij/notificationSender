using Common.DTO;
using Common.Messaging;
using Email.Service.Data;
using Email.Service.Models;
using Email.Service.Services;
using Microsoft.EntityFrameworkCore;

namespace Email.Service.Workers;

public class EmailWorker : BackgroundService
{
    private readonly ILogger<EmailWorker> _logger;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public EmailWorker(ILogger<EmailWorker> logger, IRabbitMQService rabbitMQService, 
        IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _rabbitMQService = rabbitMQService;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Email Service starting...");

            await InitializeDatabaseAsync(stoppingToken);

            _logger.LogInformation("Email Service started and listening for messages...");

            _rabbitMQService.StartConsuming<NotificationRequest>("email_queue", async (notification) =>
            {
                await ProcessNotification(notification);
            });

            await WaitForCancellationAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Email Service failed to start");
            _hostApplicationLifetime.StopApplication();
        }
    }

    private async Task InitializeDatabaseAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
    
        var maxRetries = 10;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to database (attempt {Attempt}/{MaxRetries})...", 
                    attempt, maxRetries);
            
                await dbContext.Database.MigrateAsync(stoppingToken);
                _logger.LogInformation("Database connection and migration successful");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to database (attempt {Attempt}/{MaxRetries})", 
                    attempt, maxRetries);

                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Unable to connect to database after {MaxRetries} attempts", maxRetries);
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessNotification(NotificationRequest notification)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

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

            bool success = await SendWithRetry(emailSender, notification, emailNotification, dbContext);

            if (success)
            {
                emailNotification.Status = "sent";
                emailNotification.SentAt = DateTime.UtcNow;
                _logger.LogInformation("Email sent successfully to {Recipient} for notification {NotificationId}", 
                    notification.Recipient, notification.Id);
            }
            else
            {
                emailNotification.Status = "failed";
                _logger.LogError("Failed to send email to {Recipient} for notification {NotificationId} after retries", 
                    notification.Recipient, notification.Id);
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email notification {NotificationId}", notification.Id);
        }
    }

    private async Task<bool> SendWithRetry(IEmailSender emailSender, NotificationRequest notification, 
        EmailNotification emailNotification, EmailDbContext dbContext)
    {
        var maxRetries = 3;
        var retryDelays = new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var success = await emailSender.SendEmailAsync(
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

            if (attempt < maxRetries - 1)
            {
                await Task.Delay(retryDelays[attempt]);
            }
        }

        return false;
    }

    private async Task WaitForCancellationAsync(CancellationToken stoppingToken)
    {
        try
        {
            var tcs = new TaskCompletionSource<bool>();
            stoppingToken.Register(s => ((TaskCompletionSource<bool>)s!).SetResult(true), tcs);
            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Email Service is stopping...");
        }
    }

    public override void Dispose()
    {
        _rabbitMQService?.Dispose();
        base.Dispose();
    }
}