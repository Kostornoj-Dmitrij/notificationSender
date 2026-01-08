using Microsoft.AspNetCore.Mvc;
using Common.DTO;
using Common.Messaging;
using Microsoft.Extensions.Logging;
using Prometheus; 

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly IRabbitMQService _rabbitMQService;
    private static readonly HashSet<string> _allowedNotificationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "sms",
        "push"
    };

    private static readonly Counter _notificationsReceived = Metrics
        .CreateCounter("notifications_received_total", 
            "Total number of received notifications", 
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

    private static readonly Counter _notificationsQueued = Metrics
        .CreateCounter("notifications_queued_total", 
            "Total number of successfully queued notifications",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

    private static readonly Counter _notificationsFailed = Metrics
        .CreateCounter("notifications_failed_total", 
            "Total number of failed notifications",
            new CounterConfiguration
            {
                LabelNames = new[] { "type", "reason" }
            });

    private static readonly Histogram _notificationProcessingDuration = Metrics
        .CreateHistogram("notification_processing_duration_seconds",
            "Histogram of notification processing durations",
            new HistogramConfiguration
            {
                LabelNames = new[] { "type", "status" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 16) // От 1ms до ~32 секунд
            });

    public NotificationsController(ILogger<NotificationsController> logger, IRabbitMQService rabbitMQService)
    {
        _logger = logger;
        _rabbitMQService = rabbitMQService;
    }

    [HttpPost]
    public IActionResult SendNotification([FromBody] NotificationRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var status = "success";

        try
        {
            _logger.LogInformation("Received notification request: {NotificationId}", request.Id);

            _notificationsReceived.WithLabels(request.Type?.ToLower() ?? "unknown").Inc();

            if (string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Recipient))
            {
                status = "validation_error";
                _notificationsFailed.WithLabels(request.Type?.ToLower() ?? "unknown", "missing_fields").Inc();
                return BadRequest("Type and Recipient are required");
            }

            if (!_allowedNotificationTypes.Contains(request.Type))
            {
                status = "validation_error";
                _notificationsFailed.WithLabels(request.Type.ToLower(), "invalid_type").Inc();
                return BadRequest($"Invalid notification type. Allowed types: {string.Join(", ", _allowedNotificationTypes)}");
            }

            _rabbitMQService.PublishMessage("notifications", request);
            _notificationsQueued.WithLabels(request.Type.ToLower()).Inc();
            _logger.LogInformation("Notification queued: {NotificationId}", request.Id);

            return Accepted(new
            {
                NotificationId = request.Id,
                Status = "queued",
                Message = "Notification has been queued for processing"
            });
        }
        catch (Exception ex)
        {
            status = "server_error";
            _notificationsFailed.WithLabels(request.Type?.ToLower() ?? "unknown", "exception").Inc();
            _logger.LogError(ex, "Error processing notification request");
            return StatusCode(500, "Internal server error");
        }
        finally
        {
            stopwatch.Stop();
            _notificationProcessingDuration
                .WithLabels(request.Type?.ToLower() ?? "unknown", status)
                .Observe(stopwatch.Elapsed.TotalSeconds);
        }
    }
}