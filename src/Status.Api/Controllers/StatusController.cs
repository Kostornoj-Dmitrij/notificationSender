using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Status.Api.Services;
using Status.Api.Data;
using Common.DTO;

namespace Status.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly INotificationStatusService _statusService;
    private readonly NotificationDbContext _context;
    private readonly ILogger<StatusController> _logger;

    public StatusController(
        INotificationStatusService statusService,
        NotificationDbContext context,
        ILogger<StatusController> logger)
    {
        _statusService = statusService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("{notificationId:guid}")]
    public async Task<IActionResult> GetNotificationStatus(Guid notificationId)
    {
        _logger.LogInformation("Getting status for notification ID: {NotificationId}", notificationId);
        
        var result = await _statusService.GetNotificationStatusAsync(notificationId);
        
        if (result == null || !result.Success)
        {
            return NotFound(result ?? new StatusResponse
            {
                Success = false,
                Status = null,
                Error = $"Notification with ID {notificationId} not found"
            });
        }

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchNotifications(
        [FromQuery] string? recipient,
        [FromQuery] string? status,
        [FromQuery] string? serviceType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var notifications = await _statusService
                .SearchNotificationsAsync(recipient, status, serviceType, fromDate, toDate, page, pageSize);

            return Ok(new
            {
                success = true,
                data = notifications,
                pagination = new
                {
                    page,
                    pageSize,
                    total = notifications.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching notifications");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-7);
            var toDate = DateTime.UtcNow;

            var notifications = await _statusService
                .GetNotificationsByDateRangeAsync(fromDate, toDate, page, pageSize);

            return Ok(new
            {
                success = true,
                data = notifications,
                pagination = new
                {
                    page,
                    pageSize,
                    total = notifications.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent notifications");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            var defaultFromDate = fromDate ?? DateTime.UtcNow.AddDays(-7);
            var defaultToDate = toDate ?? DateTime.UtcNow;

            var statistics = await _statusService.GetStatisticsAsync(defaultFromDate, defaultToDate);

            return Ok(new
            {
                success = true,
                statistics,
                period = new
                {
                    fromDate = defaultFromDate,
                    toDate = defaultToDate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Notification Status API"
        });
    }

    [HttpGet("db-health")]
    public async Task<IActionResult> DatabaseHealthCheck()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            
            return Ok(new
            {
                status = canConnect ? "healthy" : "unhealthy",
                database = "postgresql",
                connected = canConnect,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "postgresql",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}