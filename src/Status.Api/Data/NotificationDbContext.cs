using Microsoft.EntityFrameworkCore;
using Status.Api.Models;

namespace Status.Api.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) 
        : base(options)
    {
    }

    public DbSet<EmailNotification> EmailNotifications { get; set; }
    public DbSet<SmsNotification> SmsNotifications { get; set; }
    public DbSet<PushNotification> PushNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Используем атрибуты для конфигурации, но добавляем индексы
        modelBuilder.Entity<EmailNotification>()
            .HasIndex(e => e.NotificationId)
            .HasDatabaseName("ix_email_notifications_NotificationId");

        modelBuilder.Entity<SmsNotification>()
            .HasIndex(s => s.NotificationId)
            .HasDatabaseName("ix_sms_notifications_NotificationId");

        modelBuilder.Entity<PushNotification>()
            .HasIndex(p => p.NotificationId)
            .HasDatabaseName("ix_push_notifications_NotificationId");
    }
}