using Common.DTO;

namespace Status.Api.Services;

public interface INotificationStatusService
{
    Task<StatusResponse?> GetNotificationStatusAsync(Guid notificationId);
    Task<List<AggregatedNotificationStatus>> GetNotificationsByDateRangeAsync(
        DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50);
    Task<List<AggregatedNotificationStatus>> SearchNotificationsAsync(
        string? recipient = null,
        string? status = null,
        string? serviceType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50);
    Task<Dictionary<string, int>> GetStatisticsAsync(DateTime fromDate, DateTime toDate);
}