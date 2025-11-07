namespace Common.DTO;

public class NotificationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } // "email", "sms", "push"
    public string Recipient { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
