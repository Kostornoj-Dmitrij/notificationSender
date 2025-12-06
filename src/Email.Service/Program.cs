using Common.Messaging;
using Email.Service.Settings;
using Email.Service.Data;
using Email.Service.Services;
using Email.Service.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = Common.Utils.LoggerConfiguration.CreateLogger("Email.Service");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.Services.AddSerilog();

    builder.Services.AddSingleton<SmtpSettings>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        var senderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL");
        Console.WriteLine($"DEBUG: SMTP_USERNAME from Environment: '{username}'");
        Console.WriteLine($"DEBUG: SMTP_PASSWORD from Environment: '{(string.IsNullOrEmpty(password) ? "EMPTY" : "PRESENT")}'");
        Console.WriteLine($"DEBUG: SMTP_SENDER_EMAIL from Environment: '{senderEmail}'");
        
        var settings = new SmtpSettings
        {
            Username = username ?? string.Empty,
            Password = password ?? string.Empty,
            SenderEmail = senderEmail ?? string.Empty,
            
            Server = config["SmtpSettings:Server"] ?? "localhost",
            Port = int.TryParse(config["SmtpSettings:Port"], out var port) ? port : 587,
            SenderName = config["SmtpSettings:SenderName"] ?? "Notification Service",
            UseSsl = bool.TryParse(config["SmtpSettings:UseSsl"], out var useSsl) && useSsl,
            TestMode = bool.TryParse(config["SmtpSettings:TestMode"], out var testMode) && testMode
        };
        Console.WriteLine("✅ Final SmtpSettings:");
        Console.WriteLine($"  Server: {settings.Server}");
        Console.WriteLine($"  Port: {settings.Port}");
        Console.WriteLine($"  Username: {settings.Username}");
        Console.WriteLine($"  Password: {settings.Password}");
        Console.WriteLine($"  SenderEmail: {settings.SenderEmail}");
        Console.WriteLine($"  SenderName: {settings.SenderName}");
        Console.WriteLine($"  UseSsl: {settings.UseSsl}");
        Console.WriteLine($"  TestMode: {settings.TestMode}");
        return settings;
    });

    builder.Services.Configure<RetrySettings>(builder.Configuration.GetSection("RetrySettings"));
    builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

    builder.Services.AddDbContext<EmailDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    });

    builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

    var serviceProvider = builder.Services.BuildServiceProvider();
    var smtpSettings = serviceProvider.GetRequiredService<SmtpSettings>();
    bool hasValidCredentials = !string.IsNullOrEmpty(smtpSettings.Username) && 
                               !string.IsNullOrEmpty(smtpSettings.Password) &&
                               !string.IsNullOrEmpty(smtpSettings.SenderEmail);

    if (smtpSettings.TestMode || !hasValidCredentials)
    {
        builder.Services.AddScoped<IEmailSender, FakeEmailSender>();
        Log.Information("Using FAKE email sender for testing");
        Log.Information("To use real SMTP, set SMTP_TEST_MODE=false and configure SMTP_USERNAME/SMTP_PASSWORD in .env");
    }
    else
    {
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        Log.Information("Using REAL SMTP email sender");
    }

    builder.Services.AddScoped<EmailProcessingService>();
    builder.Services.AddHostedService<EmailWorker>();

    var host = builder.Build();
    using (var scope = host.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
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