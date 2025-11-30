using Common.Messaging;
using Push.Service.Data;
using Push.Service.Services;
using Microsoft.EntityFrameworkCore;

namespace Push.Service.Workers;

public class PushWorker : BackgroundService
{
    private readonly ILogger<PushWorker> _logger;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public PushWorker(
        ILogger<PushWorker> logger,
        IRabbitMQService rabbitMQService,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime)
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
            _logger.LogInformation("Push Service starting...");

            await InitializeDatabaseAsync(stoppingToken);

            _logger.LogInformation("Push Service started and listening for messages...");

            _rabbitMQService.StartConsuming<Common.DTO.NotificationRequest>("push_queue", async (notification) =>
            {
                await ProcessNotificationAsync(notification);
            });

            await WaitForCancellationAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Push Service failed to start");
            _hostApplicationLifetime.StopApplication();
        }
    }

    private async Task InitializeDatabaseAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PushDbContext>();
        var retrySettings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Settings.RetrySettings>>().Value;

        for (int attempt = 1; attempt <= retrySettings.DatabaseMaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to database (attempt {Attempt}/{MaxRetries})...",
                    attempt, retrySettings.DatabaseMaxRetries);

                await dbContext.Database.MigrateAsync(stoppingToken);
                _logger.LogInformation("Database connection and migration successful");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to database (attempt {Attempt}/{MaxRetries})",
                    attempt, retrySettings.DatabaseMaxRetries);

                if (attempt == retrySettings.DatabaseMaxRetries)
                {
                    _logger.LogError(ex, "Unable to connect to database after {MaxRetries} attempts",
                        retrySettings.DatabaseMaxRetries);
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(retrySettings.DatabaseRetryDelayInSeconds), stoppingToken);
            }
        }
    }

    private async Task ProcessNotificationAsync(Common.DTO.NotificationRequest notification)
    {
        using var scope = _serviceProvider.CreateScope();
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
        _rabbitMQService.Dispose();
        base.Dispose();
    }
}