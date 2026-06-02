namespace StudentPortal.Services;

public interface IImageStorageService
{
    Task<ImageUploadResult> UploadImageAsync(IFormFile image, CancellationToken cancellationToken = default);
}
