using Common.Messaging;
using Push.Service.Data;
using Push.Service.Services;
using Microsoft.EntityFrameworkCore;

namespace Push.Service.Workers;

public class PushWorker(
    ILogger<PushWorker> logger,
    IRabbitMQService rabbitMqService,
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Push Service starting...");

            await InitializeDatabaseAsync(stoppingToken);

            logger.LogInformation("Push Service started and listening for messages...");

            rabbitMqService.StartConsuming<Common.DTO.NotificationRequest>("push_queue", async void (notification) =>
            {
                await ProcessNotificationAsync(notification);
            });

            await WaitForCancellationAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Push Service failed to start");
            hostApplicationLifetime.StopApplication();
        }
    }

    private async Task InitializeDatabaseAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PushDbContext>();
        var retrySettings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Settings.RetrySettings>>().Value;

        for (int attempt = 1; attempt <= retrySettings.DatabaseMaxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to connect to database (attempt {Attempt}/{MaxRetries})...",
                    attempt, retrySettings.DatabaseMaxRetries);

                await dbContext.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Database connection and migration successful");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to connect to database (attempt {Attempt}/{MaxRetries})",
                    attempt, retrySettings.DatabaseMaxRetries);

                if (attempt == retrySettings.DatabaseMaxRetries)
                {
                    logger.LogError(ex, "Unable to connect to database after {MaxRetries} attempts",
                        retrySettings.DatabaseMaxRetries);
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(retrySettings.DatabaseRetryDelayInSeconds), stoppingToken);
            }
        }
    }

    private async Task ProcessNotificationAsync(Common.DTO.NotificationRequest notification)
    {
        using var scope = serviceProvider.CreateScope();
        var pushProcessingService = scope.ServiceProvider.GetRequiredService<PushProcessingService>();
        
        await pushProcessingService.ProcessNotificationAsync(notification);
    }

    private static async Task WaitForCancellationAsync(CancellationToken stoppingToken)
    {
        try
        {
            var tcs = new TaskCompletionSource<bool>();
            stoppingToken.Register(s => ((TaskCompletionSource<bool>)s!).SetResult(true), tcs);
            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    public override void Dispose()
    {
        rabbitMqService.Dispose();
        base.Dispose();
    }
}