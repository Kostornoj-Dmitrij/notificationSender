using Email.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace Email.Service.Data;

public class EmailDbContext(DbContextOptions<EmailDbContext> options) : DbContext(options)
{
    public DbSet<EmailNotification> EmailNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailNotification>().ToTable("email_notifications");
        
        modelBuilder.Entity<EmailNotification>()
            .HasIndex(e => e.NotificationId)
            .IsUnique();
            
        modelBuilder.Entity<EmailNotification>()
            .HasIndex(e => e.Status);
    }
}