using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Models;
using StudentPortal.Models.ViewModels;

namespace StudentPortal.Controllers;

public class AdminController(ApplicationDbContext dbContext) : Controller
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
        TempData["SuccessMessage"] = "Class room created.";
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
        item.Reviews.Add(new ContentReview
        {
            Decision = ContentStatus.Approved,
            ReviewedByName = "Admin",
            AdminNote = "Approved for class room sharing."
        });

        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Content approved.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? adminNote)
    {
        var item = await dbContext.ContentItems.FirstOrDefaultAsync(item => item.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = ContentStatus.Rejected;
        item.ReviewedAt = DateTime.UtcNow;
        item.Reviews.Add(new ContentReview
        {
            Decision = ContentStatus.Rejected,
            ReviewedByName = "Admin",
            AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Rejected by admin." : adminNote.Trim()
        });

        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Content rejected.";
        return RedirectToAction(nameof(Pending));
    }
}
