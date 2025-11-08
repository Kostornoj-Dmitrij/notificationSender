using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Push.Service.Models;

[Table("push_notifications")]
public class PushNotification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public Guid NotificationId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DeviceToken { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Platform { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    [MaxLength(50)]
    public string ServiceType { get; set; } = "push";
}