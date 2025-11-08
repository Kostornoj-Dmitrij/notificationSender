namespace Email.Service.Settings;

public class RetrySettings
{
    public int MaxRetries { get; set; } = 3;
    public int[] RetryDelaysInSeconds { get; set; } = [5, 15, 30];
    public int DatabaseMaxRetries { get; set; } = 10;
    public int DatabaseRetryDelayInSeconds { get; set; } = 5;
}