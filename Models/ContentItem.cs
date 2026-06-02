namespace StudentPortal.Models;

public class ContentItem
{
    public int Id { get; set; }

    public int ClassRoomId { get; set; }

    public ClassRoom? ClassRoom { get; set; }

    public string SubmittedByName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ExternalUrl { get; set; }

    public ContentType ContentType { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public ICollection<ContentFile> Files { get; set; } = [];
}
