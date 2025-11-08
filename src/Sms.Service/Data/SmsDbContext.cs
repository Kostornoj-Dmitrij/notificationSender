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
    }
}