namespace Sms.Service.Services;

public interface ISmsSender
{
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}