using Common.Messaging;
using Sms.Service.Settings;
using Sms.Service.Data;
using Sms.Service.Services;
using Sms.Service.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = Common.Utils.LoggerConfiguration.CreateLogger("Sms.Service");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.Services.AddSerilog();

    builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("SmsSettings"));
    builder.Services.Configure<RetrySettings>(builder.Configuration.GetSection("RetrySettings"));
    builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

    builder.Services.AddDbContext<SmsDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    });

    builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

    var smsSettings = builder.Configuration.GetSection("SmsSettings").Get<SmsSettings>();
    if (smsSettings?.TestMode == true)
    {
        builder.Services.AddScoped<ISmsSender, FakeSmsSender>();
        Console.WriteLine("Using FAKE SMS sender for testing");
    }
    else
    {
        builder.Services.AddScoped<ISmsSender, TwilioSmsSender>();
        Console.WriteLine("Using REAL SMS sender (Twilio)");
    }

    builder.Services.AddScoped<SmsProcessingService>();

    builder.Services.AddHostedService<SmsWorker>();

    var host = builder.Build();
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