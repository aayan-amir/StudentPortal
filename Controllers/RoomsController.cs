using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using StudentPortal.Models.ViewModels;
using StudentPortal.Services;

namespace StudentPortal.Controllers;

public class RoomsController(
    ApplicationDbContext dbContext,
    IFileStorageService fileStorageService) : Controller
{
    private const string UploadedFileFieldName = "Submission.UploadedFile";

    public async Task<IActionResult> Index()
    {
        var rooms = await dbContext.ClassRooms
            .Include(room => room.ContentItems.Where(item => item.Status == ContentStatus.Approved))
            .OrderBy(room => room.Subject)
            .ThenBy(room => room.Name)
            .ToListAsync();

        return View(rooms);
    }

    public async Task<IActionResult> Details(int id)
    {
        var room = await dbContext.ClassRooms.FirstOrDefaultAsync(room => room.Id == id);
        if (room is null)
        {
            return NotFound();
        }

        var content = await dbContext.ContentItems
            .Include(item => item.Files)
            .Where(item => item.ClassRoomId == id && item.Status == ContentStatus.Approved)
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync();

        return View(new RoomDetailsViewModel
        {
            Room = room,
            ApprovedContent = content,
            Submission = new ContentSubmissionInput { ClassRoomId = room.Id }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([Bind(Prefix = "Submission")] ContentSubmissionInput input, CancellationToken cancellationToken)
    {
        var room = await dbContext.ClassRooms.FirstOrDefaultAsync(room => room.Id == input.ClassRoomId, cancellationToken);
        if (room is null)
        {
            return NotFound();
        }

        var requiresUpload = input.ContentType is ContentType.Image or ContentType.Pdf;

        if (requiresUpload && input.UploadedFile is null)
        {
            ModelState.AddModelError(UploadedFileFieldName, "Choose a file to upload.");
        }

        if (input.UploadedFile is not null)
        {
            if (input.ContentType == ContentType.Image && input.UploadedFile.ContentType == "application/pdf")
            {
                ModelState.AddModelError(UploadedFileFieldName, "Choose an image file for image submissions.");
            }

            if (input.ContentType == ContentType.Pdf && input.UploadedFile.ContentType != "application/pdf")
            {
                ModelState.AddModelError(UploadedFileFieldName, "Choose a PDF file for PDF submissions.");
            }
        }

        if (!ModelState.IsValid)
        {
            return await DetailsViewWithSubmissionAsync(room, input, cancellationToken);
        }

        var item = new ContentItem
        {
            ClassRoomId = input.ClassRoomId,
            SubmittedByName = input.SubmittedByName.Trim(),
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            ContentType = input.ContentType,
            ExternalUrl = input.ExternalUrl,
            Status = ContentStatus.Pending
        };

        if (requiresUpload && input.UploadedFile is not null)
        {
            FileUploadResult uploadedFile;

            try
            {
                uploadedFile = await fileStorageService.UploadFileAsync(input.UploadedFile, cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                ModelState.AddModelError(UploadedFileFieldName, ex is TaskCanceledException
                    ? "File upload timed out. Try a smaller file or submit again."
                    : ex.Message);

                return await DetailsViewWithSubmissionAsync(room, input, cancellationToken);
            }

            item.Files.Add(new ContentFile
            {
                Provider = uploadedFile.Provider,
                ResourceType = uploadedFile.ResourceType,
                PublicId = uploadedFile.PublicId,
                Url = uploadedFile.Url,
                SecureUrl = uploadedFile.SecureUrl,
                OriginalFileName = uploadedFile.OriginalFileName,
                MimeType = uploadedFile.MimeType,
                FileSize = uploadedFile.FileSize
            });
        }

        dbContext.ContentItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Your content was submitted for admin approval.";
        return RedirectToAction(nameof(Details), new { id = input.ClassRoomId });
    }

    private async Task<IActionResult> DetailsViewWithSubmissionAsync(
        ClassRoom room,
        ContentSubmissionInput input,
        CancellationToken cancellationToken)
    {
        var approvedContent = await dbContext.ContentItems
            .Include(item => item.Files)
            .Where(item => item.ClassRoomId == input.ClassRoomId && item.Status == ContentStatus.Approved)
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync(cancellationToken);

        return View("Details", new RoomDetailsViewModel
        {
            Room = room,
            ApprovedContent = approvedContent,
            Submission = input
        });
    }
}
