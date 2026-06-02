namespace StudentPortal.Services;

public class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string UploadPreset { get; set; } = "student_portal_unsigned";

    public string Folder { get; set; } = "student-portal";
}
