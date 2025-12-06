namespace Push.Service.Settings;

public class RetrySettings
{
    public int MaxRetries { get; init; } = 3;
    public int[] RetryDelaysInSeconds { get; init; } = [5, 15, 30];
    public int DatabaseMaxRetries { get; init; } = 10;
    public int DatabaseRetryDelayInSeconds { get; init; } = 5;
}