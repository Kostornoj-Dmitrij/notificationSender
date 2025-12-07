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

    var configuration = builder.Configuration;
    var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
    var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
    var senderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL");
    
    var finalSmtpSettings = new SmtpSettings
    {
        Username = username ?? string.Empty,
        Password = password ?? string.Empty,
        SenderEmail = senderEmail ?? string.Empty,
        
        Server = configuration["SmtpSettings:Server"] ?? "localhost",
        Port = int.TryParse(configuration["SmtpSettings:Port"], out var port) ? port : 587,
        SenderName = configuration["SmtpSettings:SenderName"] ?? "Notification Service",
        UseSsl = bool.TryParse(configuration["SmtpSettings:UseSsl"], out var useSsl) && useSsl,
        TestMode = bool.TryParse(configuration["SmtpSettings:TestMode"], out var testMode) && testMode
    };
    
    builder.Services.Configure<SmtpSettings>(options =>
    {
        options.Server = finalSmtpSettings.Server;
        options.Port = finalSmtpSettings.Port;
        options.Username = finalSmtpSettings.Username;
        options.Password = finalSmtpSettings.Password;
        options.SenderEmail = finalSmtpSettings.SenderEmail;
        options.SenderName = finalSmtpSettings.SenderName;
        options.UseSsl = finalSmtpSettings.UseSsl;
        options.TestMode = finalSmtpSettings.TestMode;
    });
    builder.Services.AddSingleton(finalSmtpSettings);

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