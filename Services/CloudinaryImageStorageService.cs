using System.Text.Json;
using Microsoft.Extensions.Options;

namespace StudentPortal.Services;

public class CloudinaryImageStorageService(
    HttpClient httpClient,
    IOptions<CloudinaryOptions> options) : IImageStorageService
{
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly CloudinaryOptions _options = options.Value;

    public async Task<ImageUploadResult> UploadImageAsync(
        IFormFile image,
        CancellationToken cancellationToken = default)
    {
        if (image.Length == 0)
        {
            throw new InvalidOperationException("Please choose an image before submitting.");
        }

        if (!AllowedImageTypes.Contains(image.ContentType))
        {
            throw new InvalidOperationException("Only JPG, PNG, WEBP, and GIF images are allowed.");
        }

        if (string.IsNullOrWhiteSpace(_options.CloudName))
        {
            throw new InvalidOperationException("Cloudinary cloud name is missing.");
        }

        var uploadPreset = GetUploadPreset();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(uploadPreset), "upload_preset");

        await using var stream = image.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
        form.Add(fileContent, "file", image.FileName);

        var uploadUrl = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/image/upload";
        using var response = await httpClient.PostAsync(uploadUrl, form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GetCloudinaryErrorMessage(body, uploadPreset));
        }

        using var payload = JsonDocument.Parse(body);
        var root = payload.RootElement;

        return new ImageUploadResult(
            "Cloudinary",
            root.GetProperty("public_id").GetString() ?? string.Empty,
            root.GetProperty("url").GetString() ?? string.Empty,
            root.GetProperty("secure_url").GetString() ?? string.Empty,
            image.FileName,
            image.ContentType,
            image.Length);
    }

    private string GetUploadPreset()
    {
        return string.IsNullOrWhiteSpace(_options.UploadPreset)
            ? "student_portal_unsigned"
            : _options.UploadPreset.Trim();
    }

    private static string GetCloudinaryErrorMessage(string responseBody, string uploadPreset)
    {
        try
        {
            using var payload = JsonDocument.Parse(responseBody);
            if (payload.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message))
            {
                return $"Cloudinary upload failed while using preset '{uploadPreset}': {message.GetString()}";
            }
        }
        catch (JsonException)
        {
            return $"Cloudinary upload failed while using preset '{uploadPreset}': {responseBody}";
        }

        return $"Cloudinary upload failed while using preset '{uploadPreset}': {responseBody}";
    }
}
