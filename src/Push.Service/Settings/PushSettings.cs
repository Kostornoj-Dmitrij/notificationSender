namespace Push.Service.Settings;

public class PushSettings
{
    public string FirebaseProjectId { get; init; } = string.Empty;
    public string FirebasePrivateKeyId { get; init; } = string.Empty;
    public string FirebasePrivateKey { get; init; } = string.Empty;
    public string FirebaseClientEmail { get; init; } = string.Empty;
    public string FirebaseClientId { get; init; } = string.Empty;
    public string FirebaseClientCertUrl { get; init; } = string.Empty;
    public bool TestMode { get; init; } = true;
}