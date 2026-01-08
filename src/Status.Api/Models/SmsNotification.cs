using System.ComponentModel.DataAnnotations.Schema;

namespace Status.Api.Models;

[Table("sms_notifications")]
public class SmsNotification
{
    [Column("id")]
    public int Id { get; set; }

    [Column("notification_id")]
    public Guid NotificationId { get; set; }

    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("retry_count")]
    public int RetryCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("service_type")]
    public string ServiceType { get; set; } = "sms";

    [Column("external_id")]
    public Guid? ExternalId { get; set; }

    [Column("last_status_check")]
    public DateTime? LastStatusCheck { get; set; }

    [Column("final_status")]
    public string? FinalStatus { get; set; }
}