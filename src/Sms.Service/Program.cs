using Common.Messaging;
using Sms.Service.Settings;
using Sms.Service.Data;
using Sms.Service.Services;
using Sms.Service.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DotNetEnv;

Log.Logger = Common.Utils.LoggerConfiguration.CreateLogger("Sms.Service");

try
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
    if (File.Exists(envPath))
    {
        DotNetEnv.Env.Load(envPath);
    }
    else
    {
        DotNetEnv.Env.Load();
    }

    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.Services.AddSerilog();

    builder.Services.AddSingleton<SmsSettings>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new SmsSettings
        {
            Username = Environment.GetEnvironmentVariable("SMS_USERNAME") ?? config["SmsSettings:Username"],
            Password = Environment.GetEnvironmentVariable("SMS_PASSWORD") ?? config["SmsSettings:Password"],
            BaseUrl = config["SmsSettings:BaseUrl"] ?? "https://online.sigmasms.ru/",
            Sender = config["SmsSettings:Sender"] ?? "B-Media",
            TestMode = bool.TryParse(config["SmsSettings:TestMode"], out var testMode) && testMode,
            TokenRefreshMinutes = int.TryParse(config["SmsSettings:TokenRefreshMinutes"], out var minutes)
                ? minutes : 50
        };
    });

    builder.Services.Configure<RetrySettings>(builder.Configuration.GetSection("RetrySettings"));
    builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

    builder.Services.AddDbContext<SmsDbContext>(options =>
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    });

    builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

    var serviceProvider = builder.Services.BuildServiceProvider();
    var smsSettings = serviceProvider.GetRequiredService<SmsSettings>();

    if (smsSettings?.TestMode == true)
    {
        builder.Services.AddScoped<ISmsSender, FakeSmsSender>();
        Console.WriteLine("Using FAKE SMS sender for testing");
    }
    else
    {
        if (string.IsNullOrEmpty(smsSettings?.Username) || string.IsNullOrEmpty(smsSettings?.Password))
        {
            throw new InvalidOperationException(
                "SMS credentials are not configured. Set SMS_USERNAME and SMS_PASSWORD in .env file");
        }

        builder.Services.AddScoped<ISmsSender, SigmaSmsSender>();
        Console.WriteLine("Using REAL SMS sender (SigmaSMS)");
    }

    builder.Services.AddScoped<SmsProcessingService>();
    builder.Services.AddHostedService<SmsWorker>();

    var host = builder.Build();

    using (var scope = host.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<SmsDbContext>();
            await context.Database.MigrateAsync();
            Log.Information("ћиграции базы данных успешно применены");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Ќе удалось применить миграции базы данных");
            throw;
        }
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}