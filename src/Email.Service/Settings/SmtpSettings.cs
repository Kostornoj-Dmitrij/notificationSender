namespace Email.Service.Settings;

public class SmtpSettings
{
    public string Server { get; init; } = "localhost";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string SenderEmail { get; init; } = string.Empty;
    public string SenderName { get; init; } = "Notification Service";
    public bool UseSsl { get; init; } = true;
    public bool TestMode { get; init; } = true;
}