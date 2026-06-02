using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;

namespace StudentPortal.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    public DbSet<ContentFile> ContentFiles => Set<ContentFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClassRoom>(entity =>
        {
            entity.ToTable("class_rooms");
            entity.Property(room => room.Id).HasColumnName("id");
            entity.Property(room => room.Name).HasMaxLength(100).IsRequired();
            entity.Property(room => room.Name).HasColumnName("name");
            entity.Property(room => room.Section).HasMaxLength(60).IsRequired();
            entity.Property(room => room.Section).HasColumnName("section");
            entity.Property(room => room.Subject).HasMaxLength(100).IsRequired();
            entity.Property(room => room.Subject).HasColumnName("subject");
            entity.Property(room => room.Description).HasMaxLength(500);
            entity.Property(room => room.Description).HasColumnName("description");
            entity.Property(room => room.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(room => new { room.Name, room.Section, room.Subject }).IsUnique();
        });

        modelBuilder.Entity<ContentItem>(entity =>
        {
            entity.ToTable("content_items");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.ClassRoomId).HasColumnName("class_room_id");
            entity.Property(item => item.SubmittedByName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.SubmittedByName).HasColumnName("submitted_by_name");
            entity.Property(item => item.Title).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Title).HasColumnName("title");
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.Property(item => item.Description).HasColumnName("description");
            entity.Property(item => item.ExternalUrl).HasMaxLength(600);
            entity.Property(item => item.ExternalUrl).HasColumnName("external_url");
            entity.Property(item => item.ContentType).HasConversion<string>().HasMaxLength(40).HasColumnName("content_type");
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(40).HasColumnName("status");
            entity.Property(item => item.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(item => item.ReviewedAt).HasColumnName("reviewed_at");
            entity.HasIndex(item => new { item.ClassRoomId, item.Status, item.SubmittedAt });
            entity.HasOne(item => item.ClassRoom)
                .WithMany(room => room.ContentItems)
                .HasForeignKey(item => item.ClassRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContentFile>(entity =>
        {
            entity.ToTable("content_files");
            entity.Property(file => file.Id).HasColumnName("id");
            entity.Property(file => file.ContentItemId).HasColumnName("content_item_id");
            entity.Property(file => file.Provider).HasMaxLength(60).IsRequired();
            entity.Property(file => file.Provider).HasColumnName("provider");
            entity.Property(file => file.ResourceType).HasMaxLength(30).IsRequired();
            entity.Property(file => file.ResourceType).HasColumnName("resource_type");
            entity.Property(file => file.PublicId).HasMaxLength(250).IsRequired();
            entity.Property(file => file.PublicId).HasColumnName("public_id");
            entity.Property(file => file.Url).HasMaxLength(800).IsRequired();
            entity.Property(file => file.Url).HasColumnName("url");
            entity.Property(file => file.SecureUrl).HasMaxLength(800).IsRequired();
            entity.Property(file => file.SecureUrl).HasColumnName("secure_url");
            entity.Property(file => file.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(file => file.OriginalFileName).HasColumnName("original_file_name");
            entity.Property(file => file.MimeType).HasMaxLength(80).IsRequired();
            entity.Property(file => file.MimeType).HasColumnName("mime_type");
            entity.Property(file => file.FileSize).HasColumnName("file_size");
            entity.Property(file => file.UploadedAt).HasColumnName("uploaded_at");
            entity.HasIndex(file => file.PublicId);
            entity.HasOne(file => file.ContentItem)
                .WithMany(item => item.Files)
                .HasForeignKey(file => file.ContentItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
