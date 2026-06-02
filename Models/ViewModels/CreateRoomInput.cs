using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models.ViewModels;

public class CreateRoomInput
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Section { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}
