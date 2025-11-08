using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sms.Service.Models;

[Table("sms_notifications")]
public class SmsNotification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public Guid NotificationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; }

    [Required]
    public string Message { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    [MaxLength(50)]
    public string ServiceType { get; set; } = "sms";
}