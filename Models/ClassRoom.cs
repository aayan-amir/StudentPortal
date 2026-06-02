namespace StudentPortal.Models;

public class ClassRoom
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Section { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ContentItem> ContentItems { get; set; } = [];
}
