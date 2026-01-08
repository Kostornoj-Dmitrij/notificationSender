using System.ComponentModel.DataAnnotations.Schema;

namespace Status.Api.Models;

[Table("email_notifications")]
public class EmailNotification
{
    [Column("Id")]
    public int Id { get; set; }

    [Column("NotificationId")]
    public Guid NotificationId { get; set; }

    [Column("Recipient")]
    public string Recipient { get; set; } = string.Empty;

    [Column("Subject")]
    public string Subject { get; set; } = string.Empty;

    [Column("Message")]
    public string Message { get; set; } = string.Empty;

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
    public string ServiceType { get; set; } = "email";
}