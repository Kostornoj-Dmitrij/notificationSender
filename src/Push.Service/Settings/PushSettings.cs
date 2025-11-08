namespace Push.Service.Settings;

public class PushSettings
{
    public string FirebaseProjectId { get; set; } = string.Empty;
    public string FirebasePrivateKeyId { get; set; } = string.Empty;
    public string FirebasePrivateKey { get; set; } = string.Empty;
    public string FirebaseClientEmail { get; set; } = string.Empty;
    public string FirebaseClientId { get; set; } = string.Empty;
    public string FirebaseClientCertUrl { get; set; } = string.Empty;
    public bool TestMode { get; set; } = true;
}