namespace Push.Service.Settings;

public class PushSettings
{
    public bool TestMode { get; set; } = true;
    public string PushTesterUrl { get; set; } = "http://push.tester:8082";
}