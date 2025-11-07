namespace Common.DTO;

public class NotificationStatus
{
    public Guid NotificationId { get; set; }
    public string ServiceType { get; set; } // "email", "sms", "push"
    public string Status { get; set; } // "pending", "sent", "failed", "delivered"
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public string Recipient { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }
}

public class AggregatedNotificationStatus
{
    public Guid NotificationId { get; set; }
    public string OverallStatus { get; set; }
    public List<NotificationStatus> ServiceStatuses { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class StatusResponse
{
    public bool Success { get; set; }
    public AggregatedNotificationStatus Status { get; set; }
    public string Error { get; set; }
}
