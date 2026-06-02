using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using StudentPortal.Data;
using StudentPortal.Models;

namespace StudentPortal.Controllers;

public class FilesController(
    ApplicationDbContext dbContext,
    IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Open(int id, CancellationToken cancellationToken)
    {
        var file = await FindVisibleFileAsync(id, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        if (!string.Equals(file.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(file.SecureUrl);
        }

        var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(file.SecureUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Could not load the PDF.");
        }

        var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = EnsurePdfExtension(file.OriginalFileName);

        Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
        {
            FileNameStar = fileName
        }.ToString();

        return File(fileBytes, "application/pdf");
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var file = await FindVisibleFileAsync(id, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(file.SecureUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Could not download the file.");
        }

        var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = string.IsNullOrWhiteSpace(file.MimeType)
            ? "application/octet-stream"
            : file.MimeType;

        return File(fileBytes, contentType, EnsureFileExtension(file.OriginalFileName, contentType));
    }

    private async Task<ContentFile?> FindVisibleFileAsync(int id, CancellationToken cancellationToken)
    {
        var file = await dbContext.ContentFiles
            .Include(file => file.ContentItem)
            .FirstOrDefaultAsync(file => file.Id == id, cancellationToken);

        if (file is null)
        {
            return null;
        }

        return file.ContentItem?.Status == ContentStatus.Approved || User.IsInRole("Admin")
            ? file
            : null;
    }

    private static string EnsurePdfExtension(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "document.pdf";
        }

        return Path.HasExtension(safeFileName)
            ? safeFileName
            : $"{safeFileName}.pdf";
    }

    private static string EnsureFileExtension(string fileName, string contentType)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "download";
        }

        if (Path.HasExtension(safeFileName))
        {
            return safeFileName;
        }

        var extension = contentType.ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            "video/quicktime" => ".mov",
            _ => string.Empty
        };

        return $"{safeFileName}{extension}";
    }
}
