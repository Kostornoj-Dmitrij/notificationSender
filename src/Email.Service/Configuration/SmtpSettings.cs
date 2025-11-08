namespace Email.Service.Configuration;

public class SmtpSettings
{
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Notification Service";
    public bool UseSsl { get; set; } = true;
    public bool TestMode { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}