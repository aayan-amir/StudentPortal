using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models.ViewModels;

public class ContentSubmissionInput
{
    [Required]
    public int ClassRoomId { get; set; }

    [Required]
    [StringLength(120)]
    public string SubmittedByName { get; set; } = string.Empty;

    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public ContentType ContentType { get; set; } = ContentType.Note;

    [Url]
    [StringLength(600)]
    public string? ExternalUrl { get; set; }

    public IFormFile? UploadedFile { get; set; }
}
