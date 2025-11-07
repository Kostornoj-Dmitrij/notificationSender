[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly IRabbitMQService _rabbitMQService;

    public NotificationsController(ILogger<NotificationsController> logger, IRabbitMQService rabbitMQService)
    {
        _logger = logger;
        _rabbitMQService = rabbitMQService;
    }

    [HttpPost]
    public IActionResult SendNotification([FromBody] NotificationRequest request)
    {
        try
        {
            _logger.LogInformation("Received notification request: {NotificationId}", request.Id);

            // Валидация
            if (string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Recipient))
            {
                return BadRequest("Type and Recipient are required");
            }

            // Публикация в RabbitMQ
            _rabbitMQService.PublishMessage("notifications", request);

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
            _logger.LogError(ex, "Error processing notification request");
            return StatusCode(500, "Internal server error");
        }
    }
}