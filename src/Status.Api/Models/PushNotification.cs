using System.ComponentModel.DataAnnotations.Schema;

namespace Status.Api.Models;

[Table("push_notifications")]
public class PushNotification
{
    [Column("Id")]
    public int Id { get; set; }

    [Column("NotificationId")]
    public Guid NotificationId { get; set; }

    [Column("DeviceToken")]
    public string DeviceToken { get; set; } = string.Empty;

    [Column("Title")]
    public string Title { get; set; } = string.Empty;

    [Column("Message")]
    public string Message { get; set; } = string.Empty;

    [Column("Platform")]
    public string? Platform { get; set; }

    [Column("Status")]
    public string Status { get; set; } = "pending";

    [Column("ErrorMessage")]
    public string? ErrorMessage { get; set; }

    [Column("RetryCount")]
    public int RetryCount { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("SentAt")]
    public DateTime? SentAt { get; set; }

    [Column("ServiceType")]
    public string ServiceType { get; set; } = "push";
}