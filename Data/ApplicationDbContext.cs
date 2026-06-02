using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;

namespace StudentPortal.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();

    public DbSet<ClassMembership> ClassMemberships => Set<ClassMembership>();

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    public DbSet<ContentFile> ContentFiles => Set<ContentFile>();

    public DbSet<ContentReview> ContentReviews => Set<ContentReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(180).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(30).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<ClassRoom>(entity =>
        {
            entity.Property(room => room.Name).HasMaxLength(100).IsRequired();
            entity.Property(room => room.Section).HasMaxLength(60).IsRequired();
            entity.Property(room => room.Subject).HasMaxLength(100).IsRequired();
            entity.Property(room => room.Description).HasMaxLength(500);
            entity.HasIndex(room => new { room.Name, room.Section, room.Subject }).IsUnique();
        });

        modelBuilder.Entity<ClassMembership>(entity =>
        {
            entity.HasIndex(member => new { member.ClassRoomId, member.AppUserId }).IsUnique();
            entity.HasOne(member => member.ClassRoom)
                .WithMany(room => room.Members)
                .HasForeignKey(member => member.ClassRoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(member => member.AppUser)
                .WithMany(user => user.ClassMemberships)
                .HasForeignKey(member => member.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContentItem>(entity =>
        {
            entity.Property(item => item.SubmittedByName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.Property(item => item.ExternalUrl).HasMaxLength(600);
            entity.Property(item => item.ContentType).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(item => new { item.ClassRoomId, item.Status, item.SubmittedAt });
            entity.HasOne(item => item.ClassRoom)
                .WithMany(room => room.ContentItems)
                .HasForeignKey(item => item.ClassRoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.SubmittedBy)
                .WithMany(user => user.SubmittedContent)
                .HasForeignKey(item => item.SubmittedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ContentFile>(entity =>
        {
            entity.Property(file => file.Provider).HasMaxLength(60).IsRequired();
            entity.Property(file => file.PublicId).HasMaxLength(250).IsRequired();
            entity.Property(file => file.Url).HasMaxLength(800).IsRequired();
            entity.Property(file => file.SecureUrl).HasMaxLength(800).IsRequired();
            entity.Property(file => file.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(file => file.MimeType).HasMaxLength(80).IsRequired();
            entity.HasIndex(file => file.PublicId);
            entity.HasOne(file => file.ContentItem)
                .WithMany(item => item.Files)
                .HasForeignKey(file => file.ContentItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContentReview>(entity =>
        {
            entity.Property(review => review.Decision).HasConversion<string>().HasMaxLength(40);
            entity.Property(review => review.ReviewedByName).HasMaxLength(120).IsRequired();
            entity.Property(review => review.AdminNote).HasMaxLength(1000);
            entity.HasOne(review => review.ContentItem)
                .WithMany(item => item.Reviews)
                .HasForeignKey(review => review.ContentItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
