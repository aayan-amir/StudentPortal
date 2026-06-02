namespace StudentPortal.Services;

public interface IFileStorageService
{
    Task<FileUploadResult> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string publicId, string resourceType, CancellationToken cancellationToken = default);
}
