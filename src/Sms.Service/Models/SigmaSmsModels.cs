using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sms.Service.Models;

public enum SigmaMessageStatus
{
    pending,
    paused,
    processing,
    sent,
    delivered,
    seen,
    failed,

    False
}

public class SigmaAuthResponse
{
    [JsonProperty("token")]
    public string Token { get; set; } = string.Empty;
}

public class SigmaSmsRequest
{
    [JsonProperty("recipient")]
    public string Recipient { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "sms";

    [JsonProperty("payload")]
    public SigmaSmsPayload Payload { get; set; } = new();
}

public class SigmaSmsPayload
{
    [JsonProperty("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

public class SigmaSmsResponse
{
    [JsonProperty("id")]
    public Guid? Id { get; set; }

    [JsonProperty("recipient")]
    public string? Recipient { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }

    [JsonProperty("groupId")]
    public Guid? GroupId { get; set; }
}

public class SigmaMessageStatusResponse
{
    [JsonProperty("id")]
    public Guid? Id { get; set; }

    [JsonProperty("chainId")]
    public Guid? ChainId { get; set; }

    [JsonProperty("state")]
    public SigmaMessageState? State { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }
}

public class SigmaMessageState
{
    [JsonProperty("status")]
    public SigmaMessageStatus Status { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }
}

[Table("sms_notifications")]
public class SmsNotification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("notification_id")]
    public Guid NotificationId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
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

    [MaxLength(50)]
    [Column("service_type")]
    public string ServiceType { get; set; } = "sms";

    [Column("external_id")]
    public Guid? ExternalId { get; set; }

    [Column("last_status_check")]
    public DateTime? LastStatusCheck { get; set; }

    [Column("final_status")]
    public string? FinalStatus { get; set; }
}