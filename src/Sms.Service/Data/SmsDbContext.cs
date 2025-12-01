using Microsoft.EntityFrameworkCore;
using Sms.Service.Models;

namespace Sms.Service.Data;

public class SmsDbContext : DbContext
{
    public SmsDbContext(DbContextOptions<SmsDbContext> options) : base(options)
    {
    }

    public DbSet<SmsNotification> SmsNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SmsNotification>().ToTable("sms_notifications");

        modelBuilder.Entity<SmsNotification>()
            .HasIndex(e => e.NotificationId)
            .IsUnique();

        modelBuilder.Entity<SmsNotification>()
            .HasIndex(e => e.Status);

        modelBuilder.Entity<SmsNotification>()
            .HasIndex(e => e.ExternalId);

        modelBuilder.Entity<SmsNotification>()
            .HasIndex(e => e.LastStatusCheck);

        modelBuilder.Entity<SmsNotification>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id").IsRequired();
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Message).HasColumnName("message").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.ServiceType).HasColumnName("service_type").HasMaxLength(50);
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.LastStatusCheck).HasColumnName("last_status_check");
            entity.Property(e => e.FinalStatus).HasColumnName("final_status").HasMaxLength(20);
        });
    }
}