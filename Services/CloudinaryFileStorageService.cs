using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace StudentPortal.Services;

public class CloudinaryFileStorageService(
    HttpClient httpClient,
    IOptions<CloudinaryOptions> options) : IFileStorageService
{
    private static readonly HashSet<string> AllowedFileTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/pdf"
    };

    private readonly CloudinaryOptions _options = options.Value;

    public async Task<FileUploadResult> UploadFileAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Please choose a file before submitting.");
        }

        if (!AllowedFileTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Only JPG, PNG, WEBP, GIF, and PDF files are allowed.");
        }

        EnsureCloudinarySettings();

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var publicId = BuildPublicId(file.FileName, file.ContentType);
        var signedParameters = new SortedDictionary<string, string>
        {
            ["folder"] = _options.Folder,
            ["public_id"] = publicId,
            ["timestamp"] = timestamp
        };

        var formValues = new Dictionary<string, string>
        {
            ["api_key"] = _options.ApiKey,
            ["file"] = $"data:{file.ContentType};base64,{Convert.ToBase64String(memoryStream.ToArray())}",
            ["folder"] = _options.Folder,
            ["public_id"] = publicId,
            ["timestamp"] = timestamp,
            ["signature"] = CreateSignature(signedParameters)
        };

        var cloudinaryResourceType = file.ContentType == "application/pdf" ? "raw" : "image";
        var uploadUrl = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/{cloudinaryResourceType}/upload";
        using var form = new FormUrlEncodedContent(formValues);
        using var response = await httpClient.PostAsync(uploadUrl, form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GetCloudinaryErrorMessage(body, "upload"));
        }

        using var payload = JsonDocument.Parse(body);
        var root = payload.RootElement;

        return new FileUploadResult(
            "Cloudinary",
            root.GetProperty("resource_type").GetString() ?? "image",
            root.GetProperty("public_id").GetString() ?? string.Empty,
            root.GetProperty("url").GetString() ?? string.Empty,
            root.GetProperty("secure_url").GetString() ?? string.Empty,
            file.FileName,
            file.ContentType,
            file.Length);
    }

    public async Task DeleteFileAsync(
        string publicId,
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        EnsureCloudinarySettings();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signedParameters = new SortedDictionary<string, string>
        {
            ["public_id"] = publicId,
            ["timestamp"] = timestamp
        };

        var formValues = new Dictionary<string, string>
        {
            ["api_key"] = _options.ApiKey,
            ["public_id"] = publicId,
            ["timestamp"] = timestamp,
            ["signature"] = CreateSignature(signedParameters)
        };

        var safeResourceType = string.IsNullOrWhiteSpace(resourceType) ? "image" : resourceType.Trim();
        var destroyUrl = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/{safeResourceType}/destroy";
        using var form = new FormUrlEncodedContent(formValues);
        using var response = await httpClient.PostAsync(destroyUrl, form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GetCloudinaryErrorMessage(body, "delete"));
        }

        using var payload = JsonDocument.Parse(body);
        var result = payload.RootElement.TryGetProperty("result", out var resultElement)
            ? resultElement.GetString()
            : null;

        if (result is not ("ok" or "not found"))
        {
            throw new InvalidOperationException($"Cloudinary delete failed: {body}");
        }
    }

    private void EnsureCloudinarySettings()
    {
        if (string.IsNullOrWhiteSpace(_options.CloudName))
        {
            throw new InvalidOperationException("Cloudinary cloud name is missing.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary API key or API secret is missing.");
        }
    }

    private string CreateSignature(SortedDictionary<string, string> parameters)
    {
        var payload = string.Join("&", parameters.Select(parameter => $"{parameter.Key}={parameter.Value}"));
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(payload + _options.ApiSecret))).ToLowerInvariant();
    }

    private static string BuildPublicId(string fileName, string contentType)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var cleaned = new string(nameWithoutExtension
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray());
        cleaned = string.Join("-", cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "file";
        }

        var extension = string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            ? ".pdf"
            : string.Empty;

        return $"{cleaned}-{Guid.NewGuid():N}{extension}";
    }

    private static string GetCloudinaryErrorMessage(string responseBody, string action)
    {
        try
        {
            using var payload = JsonDocument.Parse(responseBody);
            if (payload.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message))
            {
                return $"Cloudinary {action} failed: {message.GetString()}";
            }
        }
        catch (JsonException)
        {
            return $"Cloudinary {action} failed: {responseBody}";
        }

        return $"Cloudinary {action} failed: {responseBody}";
    }
}
