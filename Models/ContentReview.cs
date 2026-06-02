namespace StudentPortal.Models;

public class ContentReview
{
    public int Id { get; set; }

    public int ContentItemId { get; set; }

    public ContentItem? ContentItem { get; set; }

    public ContentStatus Decision { get; set; }

    public string ReviewedByName { get; set; } = string.Empty;

    public string AdminNote { get; set; } = string.Empty;

    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}
