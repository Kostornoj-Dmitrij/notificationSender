using Common.DTO;
using Common.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Router.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRabbitMQService _rabbitMQService;

    public Worker(ILogger<Worker> logger, IRabbitMQService rabbitMQService)
    {
        _logger = logger;
        _rabbitMQService = rabbitMQService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Router Service started");

        _rabbitMQService.StartConsuming<NotificationRequest>("notifications", RouteNotification);

        _logger.LogInformation("Router Service is listening for notifications...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void RouteNotification(NotificationRequest notification)
    {
        try
        {
            _logger.LogInformation("Routing notification: {NotificationId}, Type: {Type}",
                notification.Id, notification.Type);

            string targetQueue = notification.Type.ToLower() switch
            {
                "email" => "email_queue",
                "sms" => "sms_queue",
                "push" => "push_queue",
                _ => throw new ArgumentException($"Unknown notification type: {notification.Type}")
            };

            _rabbitMQService.PublishMessage(targetQueue, notification);

            _logger.LogInformation("Notification {NotificationId} routed to {Queue}",
                notification.Id, targetQueue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing notification {NotificationId}", notification.Id);
        }
    }
}