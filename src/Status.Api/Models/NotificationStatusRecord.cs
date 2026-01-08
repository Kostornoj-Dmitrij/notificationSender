namespace Status.Api.Models;

public class NotificationStatusRecord
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public string? Recipient { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public DateTime UpdatedAt { get; set; }
}