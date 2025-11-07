using Serilog;
using Serilog.Events;

namespace Common.Utils;

public static class LoggerConfiguration
{
    public static Serilog.ILogger CreateLogger(string serviceName)
    {
        return new Serilog.LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
            .WriteTo.File($"logs/{serviceName}-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
            .CreateLogger();
    }
}
