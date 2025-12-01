namespace Sms.Service.Settings;

public class SmsSettings
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://online.sigmasms.ru/api";
    public string Sender { get; set; } = string.Empty;
    public bool TestMode { get; set; } = true;
    public int TokenRefreshMinutes { get; set; } = 50;
}