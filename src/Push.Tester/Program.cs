using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var notifications = new List<object>();
var notificationsLock = new object();

app.MapPost("/api/push", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        
        Console.WriteLine($"📨 Received push notification: {body}");
        
        var notificationData = JsonSerializer.Deserialize<JsonElement>(body);
        var notification = new
        {
            Id = Guid.NewGuid(),
            Data = notificationData,
            ReceivedAt = DateTime.UtcNow
        };
        
        lock (notificationsLock)
        {
            notifications.Add(notification);
            
            if (notifications.Count > 100)
            {
                notifications.RemoveAt(0);
            }
        }
        
        Console.WriteLine($"💾 Stored notification. Total: {notifications.Count}");
        
        return Results.Ok(new { 
            message = "Push notification received",
            storedCount = notifications.Count
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error processing push: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/notifications", (HttpContext context) =>
{
    lock (notificationsLock)
    {
        return Results.Ok(new
        {
            status = "Success",
            count = notifications.Count,
            notifications = notifications.Select(n => new
            {
                id = ((dynamic)n).Id,
                title = ((JsonElement)((dynamic)n).Data).GetProperty("Title").GetString(),
                message = ((JsonElement)((dynamic)n).Data).GetProperty("Message").GetString(),
                timestamp = ((JsonElement)((dynamic)n).Data).GetProperty("Timestamp").GetString(),
                receivedAt = ((dynamic)n).ReceivedAt
            }).Reverse().ToList()
        });
    }
});

app.MapGet("/api/health", () => 
{
    lock (notificationsLock)
    {
        return Results.Ok(new { 
            status = "Healthy", 
            notificationCount = notifications.Count
        });
    }
});

app.MapDelete("/api/notifications", () =>
{
    lock (notificationsLock)
    {
        var count = notifications.Count;
        notifications.Clear();
        return Results.Ok(new { message = $"Cleared {count} notifications" });
    }
});

app.Run();