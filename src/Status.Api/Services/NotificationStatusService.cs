using Microsoft.EntityFrameworkCore;
using Common.DTO;
using Status.Api.Data;
using Status.Api.Models;

namespace Status.Api.Services;

public class NotificationStatusService : INotificationStatusService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<NotificationStatusService> _logger;

    public NotificationStatusService(
        NotificationDbContext context,
        ILogger<NotificationStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StatusResponse?> GetNotificationStatusAsync(Guid notificationId)
    {
        try
        {
            var serviceStatuses = new List<NotificationStatus>();

            // Получаем email статусы
            var emailRecords = await _context.EmailNotifications
                .Where(e => e.NotificationId == notificationId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();
                
            serviceStatuses.AddRange(emailRecords.Select(MapToEmailStatus));

            // Получаем SMS статусы
            var smsRecords = await _context.SmsNotifications
                .Where(s => s.NotificationId == notificationId)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
                
            serviceStatuses.AddRange(smsRecords.Select(MapToSmsStatus));

            // Получаем Push статусы
            var pushRecords = await _context.PushNotifications
                .Where(p => p.NotificationId == notificationId)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
                
            serviceStatuses.AddRange(pushRecords.Select(MapToPushStatus));

            if (!serviceStatuses.Any())
            {
                _logger.LogWarning("Notification with ID {NotificationId} not found", notificationId);
                return new StatusResponse
                {
                    Success = false,
                    Status = null,
                    Error = $"Notification with ID {notificationId} not found"
                };
            }

            // Определяем общий статус
            var overallStatus = DetermineOverallStatus(serviceStatuses);
            
            var aggregatedStatus = new AggregatedNotificationStatus
            {
                NotificationId = notificationId,
                OverallStatus = overallStatus,
                ServiceStatuses = serviceStatuses,
                CreatedAt = serviceStatuses.Min(s => s.CreatedAt),
                LastUpdated = serviceStatuses.Max(s => s.SentAt ?? s.CreatedAt)
            };

            return new StatusResponse
            {
                Success = true,
                Status = aggregatedStatus,
                Error = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification status for ID {NotificationId}", notificationId);
            return new StatusResponse
            {
                Success = false,
                Status = null,
                Error = $"Internal server error: {ex.Message}"
            };
        }
    }

    public async Task<List<AggregatedNotificationStatus>> GetNotificationsByDateRangeAsync(
        DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50)
    {
        try
        {
            var result = new List<AggregatedNotificationStatus>();

            // Получаем все notification_id за период из всех таблиц
            var allNotificationIds = new List<Guid>();

            var emailIds = await _context.EmailNotifications
                .Where(e => e.CreatedAt >= fromDate && e.CreatedAt <= toDate)
                .Select(e => e.NotificationId)
                .Distinct()
                .ToListAsync();
            allNotificationIds.AddRange(emailIds);

            var smsIds = await _context.SmsNotifications
                .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
                .Select(s => s.NotificationId)
                .Distinct()
                .ToListAsync();
            allNotificationIds.AddRange(smsIds);

            var pushIds = await _context.PushNotifications
                .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                .Select(p => p.NotificationId)
                .Distinct()
                .ToListAsync();
            allNotificationIds.AddRange(pushIds);

            // Группируем и пагинируем уникальные ID
            var uniqueIds = allNotificationIds.Distinct()
                .OrderByDescending(id => id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Для каждого notificationId получаем все статусы
            foreach (var notificationId in uniqueIds)
            {
                var serviceStatuses = new List<NotificationStatus>();

                // Email статусы
                var emailRecords = await _context.EmailNotifications
                    .Where(e => e.NotificationId == notificationId)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync();
                serviceStatuses.AddRange(emailRecords.Select(MapToEmailStatus));

                // SMS статусы
                var smsRecords = await _context.SmsNotifications
                    .Where(s => s.NotificationId == notificationId)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();
                serviceStatuses.AddRange(smsRecords.Select(MapToSmsStatus));

                // Push статусы
                var pushRecords = await _context.PushNotifications
                    .Where(p => p.NotificationId == notificationId)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
                serviceStatuses.AddRange(pushRecords.Select(MapToPushStatus));

                if (serviceStatuses.Any())
                {
                    result.Add(new AggregatedNotificationStatus
                    {
                        NotificationId = notificationId,
                        OverallStatus = DetermineOverallStatus(serviceStatuses),
                        ServiceStatuses = serviceStatuses,
                        CreatedAt = serviceStatuses.Min(s => s.CreatedAt),
                        LastUpdated = serviceStatuses.Max(s => s.SentAt ?? s.CreatedAt)
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications by date range");
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var stats = new Dictionary<string, int>();
            
            // Статистика по Email
            var emailStats = await _context.EmailNotifications
                .Where(e => e.CreatedAt >= fromDate && e.CreatedAt <= toDate)
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            
            foreach (var stat in emailStats)
            {
                stats[$"email_{stat.Status.ToLower()}"] = stat.Count;
            }
            
            // Общее количество email уведомлений
            var totalEmail = await _context.EmailNotifications
                .Where(e => e.CreatedAt >= fromDate && e.CreatedAt <= toDate)
                .CountAsync();
            stats["total_email"] = totalEmail;
            
            // Статистика по SMS
            var smsStats = await _context.SmsNotifications
                .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            
            foreach (var stat in smsStats)
            {
                stats[$"sms_{stat.Status.ToLower()}"] = stat.Count;
            }
            
            // Общее количество SMS уведомлений
            var totalSms = await _context.SmsNotifications
                .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
                .CountAsync();
            stats["total_sms"] = totalSms;
            
            // Статистика по Push
            var pushStats = await _context.PushNotifications
                .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            
            foreach (var stat in pushStats)
            {
                stats[$"push_{stat.Status.ToLower()}"] = stat.Count;
            }
            
            // Общее количество push уведомлений
            var totalPush = await _context.PushNotifications
                .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                .CountAsync();
            stats["total_push"] = totalPush;
            
            // Общее количество уникальных notification_id
            var allIds = new HashSet<Guid>();
            
            var emailIds = await _context.EmailNotifications
                .Where(e => e.CreatedAt >= fromDate && e.CreatedAt <= toDate)
                .Select(e => e.NotificationId)
                .Distinct()
                .ToListAsync();
            allIds.UnionWith(emailIds);
            
            var smsIds = await _context.SmsNotifications
                .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
                .Select(s => s.NotificationId)
                .Distinct()
                .ToListAsync();
            allIds.UnionWith(smsIds);
            
            var pushIds = await _context.PushNotifications
                .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                .Select(p => p.NotificationId)
                .Distinct()
                .ToListAsync();
            allIds.UnionWith(pushIds);
            
            stats["total_notifications"] = allIds.Count;
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics");
            throw;
        }
    }

    public async Task<List<AggregatedNotificationStatus>> SearchNotificationsAsync(
        string? recipient = null,
        string? status = null,
        string? serviceType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            var result = new List<AggregatedNotificationStatus>();
            var allNotificationIds = new HashSet<Guid>();

            // Поиск по Email
            if (string.IsNullOrEmpty(serviceType) || serviceType.ToLower() == "email")
            {
                var emailQuery = _context.EmailNotifications.AsQueryable();
                
                if (!string.IsNullOrEmpty(recipient))
                    emailQuery = emailQuery.Where(e => e.Recipient.Contains(recipient));
                
                if (!string.IsNullOrEmpty(status))
                    emailQuery = emailQuery.Where(e => e.Status == status);
                
                if (fromDate.HasValue)
                    emailQuery = emailQuery.Where(e => e.CreatedAt >= fromDate.Value);
                
                if (toDate.HasValue)
                    emailQuery = emailQuery.Where(e => e.CreatedAt <= toDate.Value);
                
                var emailIds = await emailQuery
                    .Select(e => e.NotificationId)
                    .Distinct()
                    .ToListAsync();
                
                allNotificationIds.UnionWith(emailIds);
            }

            // Поиск по SMS
            if (string.IsNullOrEmpty(serviceType) || serviceType.ToLower() == "sms")
            {
                var smsQuery = _context.SmsNotifications.AsQueryable();
                
                if (!string.IsNullOrEmpty(recipient))
                    smsQuery = smsQuery.Where(s => s.PhoneNumber.Contains(recipient));
                
                if (!string.IsNullOrEmpty(status))
                    smsQuery = smsQuery.Where(s => s.Status == status);
                
                if (fromDate.HasValue)
                    smsQuery = smsQuery.Where(s => s.CreatedAt >= fromDate.Value);
                
                if (toDate.HasValue)
                    smsQuery = smsQuery.Where(s => s.CreatedAt <= toDate.Value);
                
                var smsIds = await smsQuery
                    .Select(s => s.NotificationId)
                    .Distinct()
                    .ToListAsync();
                
                allNotificationIds.UnionWith(smsIds);
            }

            // Поиск по Push
            if (string.IsNullOrEmpty(serviceType) || serviceType.ToLower() == "push")
            {
                var pushQuery = _context.PushNotifications.AsQueryable();
                
                if (!string.IsNullOrEmpty(recipient))
                    pushQuery = pushQuery.Where(p => 
                        p.DeviceToken.Contains(recipient) || 
                        p.Title.Contains(recipient) || 
                        p.Message.Contains(recipient));
                
                if (!string.IsNullOrEmpty(status))
                    pushQuery = pushQuery.Where(p => p.Status == status);
                
                if (fromDate.HasValue)
                    pushQuery = pushQuery.Where(p => p.CreatedAt >= fromDate.Value);
                
                if (toDate.HasValue)
                    pushQuery = pushQuery.Where(p => p.CreatedAt <= toDate.Value);
                
                var pushIds = await pushQuery
                    .Select(p => p.NotificationId)
                    .Distinct()
                    .ToListAsync();
                
                allNotificationIds.UnionWith(pushIds);
            }

            // Пагинация
            var pagedIds = allNotificationIds
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Для каждого notificationId получаем все статусы
            foreach (var notificationId in pagedIds)
            {
                var serviceStatuses = new List<NotificationStatus>();

                // Email статусы
                var emailRecords = await _context.EmailNotifications
                    .Where(e => e.NotificationId == notificationId)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync();
                serviceStatuses.AddRange(emailRecords.Select(MapToEmailStatus));

                // SMS статусы
                var smsRecords = await _context.SmsNotifications
                    .Where(s => s.NotificationId == notificationId)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();
                serviceStatuses.AddRange(smsRecords.Select(MapToSmsStatus));

                // Push статусы
                var pushRecords = await _context.PushNotifications
                    .Where(p => p.NotificationId == notificationId)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
                serviceStatuses.AddRange(pushRecords.Select(MapToPushStatus));

                if (serviceStatuses.Any())
                {
                    result.Add(new AggregatedNotificationStatus
                    {
                        NotificationId = notificationId,
                        OverallStatus = DetermineOverallStatus(serviceStatuses),
                        ServiceStatuses = serviceStatuses,
                        CreatedAt = serviceStatuses.Min(s => s.CreatedAt),
                        LastUpdated = serviceStatuses.Max(s => s.SentAt ?? s.CreatedAt)
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching notifications");
            throw;
        }
    }

    private NotificationStatus MapToEmailStatus(EmailNotification record)
    {
        return new NotificationStatus
        {
            NotificationId = record.NotificationId,
            ServiceType = record.ServiceType ?? "email",
            Status = record.Status,
            ErrorMessage = record.ErrorMessage,
            CreatedAt = record.CreatedAt,
            SentAt = record.SentAt,
            RetryCount = record.RetryCount,
            Recipient = record.Recipient,
            Subject = record.Subject,
            Message = record.Message
        };
    }

    private NotificationStatus MapToSmsStatus(SmsNotification record)
    {
        return new NotificationStatus
        {
            NotificationId = record.NotificationId,
            ServiceType = record.ServiceType ?? "sms",
            Status = record.FinalStatus ?? record.Status,
            ErrorMessage = record.ErrorMessage,
            CreatedAt = record.CreatedAt,
            SentAt = record.SentAt,
            RetryCount = record.RetryCount,
            Recipient = record.PhoneNumber,
            Subject = "SMS Message",
            Message = record.Message
        };
    }

    private NotificationStatus MapToPushStatus(PushNotification record)
    {
        return new NotificationStatus
        {
            NotificationId = record.NotificationId,
            ServiceType = record.ServiceType ?? "push",
            Status = record.Status,
            ErrorMessage = record.ErrorMessage,
            CreatedAt = record.CreatedAt,
            SentAt = record.SentAt,
            RetryCount = record.RetryCount,
            Recipient = record.DeviceToken,
            Subject = record.Title,
            Message = record.Message
        };
    }

    private string DetermineOverallStatus(List<NotificationStatus> statuses)
    {
        if (!statuses.Any())
            return "unknown";

        // Если есть хотя бы один отправленный - считаем in_progress
        if (statuses.Any(s => s.Status == "sent" || s.Status == "delivered"))
        {
            // Если все отправлены - completed
            if (statuses.All(s => s.Status == "sent" || s.Status == "delivered"))
                return "completed";
            
            // Если есть хоть один failed - partially_failed
            if (statuses.Any(s => s.Status == "failed"))
                return "partially_failed";
                
            return "in_progress";
        }
            
        // Если есть хоть один failed
        if (statuses.Any(s => s.Status == "failed"))
        {
            // Если все failed
            if (statuses.All(s => s.Status == "failed"))
                return "failed";
                
            return "partially_failed";
        }
            
        // Все pending
        if (statuses.All(s => s.Status == "pending"))
            return "pending";
            
        return "in_progress";
    }
}