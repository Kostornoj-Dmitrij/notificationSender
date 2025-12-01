namespace Sms.Service.Services;

public class SmsSendResult
{
    public bool Success { get; set; }
    public Guid? ExternalId { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface ISmsSender
{
    Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}