using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using StudentPortal.Models.ViewModels;
using StudentPortal.Services;

namespace StudentPortal.Controllers;

public class RoomsController(
    ApplicationDbContext dbContext,
    IImageStorageService imageStorageService) : Controller
{
    private const string ImageFieldName = "Submission.Image";

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

        if (input.ContentType == ContentType.Image && input.Image is null)
        {
            ModelState.AddModelError(ImageFieldName, "Choose an image to upload.");
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

        if (input.ContentType == ContentType.Image && input.Image is not null)
        {
            ImageUploadResult uploadedImage;

            try
            {
                uploadedImage = await imageStorageService.UploadImageAsync(input.Image, cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                ModelState.AddModelError(ImageFieldName, ex is TaskCanceledException
                    ? "Image upload timed out. Try a smaller image or submit again."
                    : ex.Message);

                return await DetailsViewWithSubmissionAsync(room, input, cancellationToken);
            }

            item.Files.Add(new ContentFile
            {
                Provider = uploadedImage.Provider,
                PublicId = uploadedImage.PublicId,
                Url = uploadedImage.Url,
                SecureUrl = uploadedImage.SecureUrl,
                OriginalFileName = uploadedImage.OriginalFileName,
                MimeType = uploadedImage.MimeType,
                FileSize = uploadedImage.FileSize
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
