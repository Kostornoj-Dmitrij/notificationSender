using Common.Messaging;
using Push.Service.Settings;
using Push.Service.Data;
using Push.Service.Services;
using Push.Service.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = Common.Utils.LoggerConfiguration.CreateLogger("Push.Service");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.Services.AddSerilog();

    builder.Services.Configure<PushSettings>(builder.Configuration.GetSection("PushSettings"));
    builder.Services.Configure<RetrySettings>(builder.Configuration.GetSection("RetrySettings"));
    builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

    builder.Services.AddDbContext<PushDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    });

    builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

    var pushSettings = builder.Configuration.GetSection("PushSettings").Get<PushSettings>();
    if (pushSettings?.TestMode == true)
    {
        builder.Services.AddScoped<IPushSender, FakePushSender>();
        Console.WriteLine("Using FAKE push sender for testing");
    }
    else
    {
        builder.Services.AddScoped<IPushSender, FirebasePushSender>();
        Console.WriteLine("Using REAL Firebase push sender");
    }

    builder.Services.AddScoped<PushProcessingService>();

    builder.Services.AddHostedService<PushWorker>();

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