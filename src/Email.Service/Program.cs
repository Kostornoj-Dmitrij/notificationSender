using Common.Messaging;
using Email.Service.Configuration;
using Email.Service.Data;
using Email.Service.Services;
using Email.Service.Workers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<EmailDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
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

builder.Services.AddHostedService<EmailWorker>();

var host = builder.Build();

await host.RunAsync();