using Microsoft.AspNetCore.Mvc;
using Common.DTO;

namespace Status.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;

        public StatusController(ILogger<StatusController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{notificationId}")]
        public ActionResult<NotificationStatus> GetStatus(Guid notificationId)
        {
            _logger.LogInformation("Getting status for notification: {NotificationId}", notificationId);

            // Заглушка. Всегда sent
            var status = new NotificationStatus
            {
                NotificationId = notificationId,
                ServiceType = "unknown",
                Status = "sent",
                ErrorMessage = null,
                SentAt = DateTime.UtcNow.AddMinutes(-1),
                RetryCount = 0
            };

            return Ok(status);
        }

        [HttpGet]
        public ActionResult<IEnumerable<NotificationStatus>> GetAllStatuses()
        {
            _logger.LogInformation("Getting all statuses");

            // Заглушка. Пустой список
            return Ok(new List<NotificationStatus>());
        }
    }
}