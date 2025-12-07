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
    
    builder.Services.AddHttpClient<HttpPushSender>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    var pushSettings = builder.Configuration.GetSection("PushSettings").Get<PushSettings>();
    if (pushSettings?.TestMode == true)
    {
        builder.Services.AddScoped<IPushSender, FakePushSender>();
        Log.Information("Using FAKE push sender for testing");
    }
    else
    {
        builder.Services.AddScoped<IPushSender, HttpPushSender>();
        Log.Information("Using REAL HTTP push sender");
    }

    builder.Services.AddScoped<PushProcessingService>();
    builder.Services.AddHostedService<PushWorker>();

    var host = builder.Build();
    using (var scope = host.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<PushDbContext>();
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to apply database migrations");
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