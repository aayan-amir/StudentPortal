using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using StudentPortal.Models.ViewModels;
using StudentPortal.Services;

namespace StudentPortal.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    ApplicationDbContext dbContext,
    IFileStorageService fileStorageService) : Controller
{
    public async Task<IActionResult> Pending()
    {
        var pendingItems = await dbContext.ContentItems
            .Include(item => item.ClassRoom)
            .Include(item => item.Files)
            .Where(item => item.Status == ContentStatus.Pending)
            .OrderBy(item => item.SubmittedAt)
            .ToListAsync();

        return View(pendingItems);
    }

    public async Task<IActionResult> Approved()
    {
        var approvedItems = await dbContext.ContentItems
            .Include(item => item.ClassRoom)
            .Include(item => item.Files)
            .Where(item => item.Status == ContentStatus.Approved)
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync();

        return View(approvedItems);
    }

    public async Task<IActionResult> Rooms()
    {
        var rooms = await dbContext.ClassRooms
            .OrderBy(room => room.Subject)
            .ThenBy(room => room.Name)
            .ToListAsync();

        return View(rooms);
    }

    public IActionResult CreateRoom()
    {
        return View(new CreateRoomInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(CreateRoomInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        dbContext.ClassRooms.Add(new ClassRoom
        {
            Name = input.Name.Trim(),
            Section = input.Section.Trim(),
            Subject = input.Subject.Trim(),
            Description = input.Description.Trim()
        });

        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Room created.";
        return RedirectToAction(nameof(Rooms));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var item = await dbContext.ContentItems.FirstOrDefaultAsync(item => item.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = ContentStatus.Approved;
        item.ReviewedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Content approved.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var item = await dbContext.ContentItems
            .Include(item => item.Files)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        if (!await DeleteStoredFilesAsync(item))
        {
            return RedirectToAction(nameof(Pending));
        }

        item.Status = ContentStatus.Rejected;
        item.ReviewedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Content rejected.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var room = await dbContext.ClassRooms
            .Include(room => room.ContentItems)
                .ThenInclude(item => item.Files)
            .FirstOrDefaultAsync(room => room.Id == id);
        if (room is null)
        {
            return NotFound();
        }

        foreach (var item in room.ContentItems)
        {
            if (!await DeleteStoredFilesAsync(item))
            {
                return RedirectToAction(nameof(Rooms));
            }
        }

        dbContext.ClassRooms.Remove(room);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Room and all related submissions were deleted.";
        return RedirectToAction(nameof(Rooms));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApproved(int id)
    {
        var item = await dbContext.ContentItems
            .Include(item => item.Files)
            .FirstOrDefaultAsync(item => item.Id == id && item.Status == ContentStatus.Approved);
        if (item is null)
        {
            return NotFound();
        }

        if (!await DeleteStoredFilesAsync(item))
        {
            return RedirectToAction(nameof(Approved));
        }

        dbContext.ContentItems.Remove(item);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Approved content deleted.";
        return RedirectToAction(nameof(Approved));
    }

    private async Task<bool> DeleteStoredFilesAsync(ContentItem item)
    {
        foreach (var file in item.Files.ToList())
        {
            try
            {
                await fileStorageService.DeleteFileAsync(file.PublicId, file.ResourceType);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                TempData["ErrorMessage"] = ex is TaskCanceledException
                    ? "Cloudinary delete timed out. Try again."
                    : ex.Message;

                return false;
            }

            dbContext.ContentFiles.Remove(file);
        }

        return true;
    }
}
