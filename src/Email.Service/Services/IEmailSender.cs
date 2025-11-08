namespace Email.Service.Services;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default);
}