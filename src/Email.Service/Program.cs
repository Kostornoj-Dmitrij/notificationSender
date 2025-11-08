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

    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
    builder.Services.Configure<RetrySettings>(builder.Configuration.GetSection("RetrySettings"));
    builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

    builder.Services.AddDbContext<EmailDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    });

    builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

    var smtpSettings = builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
    if (smtpSettings?.TestMode == true)
    {
        builder.Services.AddScoped<IEmailSender, FakeEmailSender>();
        Console.WriteLine("Using FAKE email sender for testing");
    }
    else
    {
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        Console.WriteLine("Using REAL SMTP email sender");
    }

    builder.Services.AddScoped<EmailProcessingService>();

    builder.Services.AddHostedService<EmailWorker>();

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