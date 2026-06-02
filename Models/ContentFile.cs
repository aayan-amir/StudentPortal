namespace StudentPortal.Models;

public class ContentFile
{
    public int Id { get; set; }

    public int ContentItemId { get; set; }

    public ContentItem? ContentItem { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string PublicId { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string SecureUrl { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
