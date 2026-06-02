namespace StudentPortal.Services;

public record ImageUploadResult(
    string Provider,
    string PublicId,
    string Url,
    string SecureUrl,
    string OriginalFileName,
    string MimeType,
    long FileSize);
