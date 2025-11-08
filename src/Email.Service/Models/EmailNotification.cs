using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Email.Service.Models;

[Table("email_notifications")]
public class EmailNotification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public Guid NotificationId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Recipient { get; set; }

    [Required]
    public string Subject { get; set; }

    public string Message { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    [MaxLength(50)]
    public string ServiceType { get; set; } = "email";
}