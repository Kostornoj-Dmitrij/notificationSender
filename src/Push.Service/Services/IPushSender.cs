namespace Push.Service.Services;

public interface IPushSender
{
    Task<bool> SendPushAsync(string deviceToken, string title, string message, string? platform = null, CancellationToken cancellationToken = default);
}