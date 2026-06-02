namespace StudentPortal.Models.ViewModels;

public class RoomDetailsViewModel
{
    public ClassRoom Room { get; set; } = new();

    public IReadOnlyList<ContentItem> ApprovedContent { get; set; } = [];

    public ContentSubmissionInput Submission { get; set; } = new();
}
