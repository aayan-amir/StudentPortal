namespace StudentPortal.Services;

public record FileUploadResult(
    string Provider,
    string ResourceType,
    string PublicId,
    string Url,
    string SecureUrl,
    string OriginalFileName,
    string MimeType,
    long FileSize);
