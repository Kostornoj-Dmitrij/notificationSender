using Microsoft.EntityFrameworkCore;
using Push.Service.Models;

namespace Push.Service.Data;

public class PushDbContext(DbContextOptions<PushDbContext> options) : DbContext(options)
{
    public DbSet<PushNotification> PushNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PushNotification>().ToTable("push_notifications");
        
        modelBuilder.Entity<PushNotification>()
            .HasIndex(e => e.NotificationId)
            .IsUnique();
            
        modelBuilder.Entity<PushNotification>()
            .HasIndex(e => e.Status);
    }
}