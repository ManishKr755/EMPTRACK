using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using EMPTRACK.Services;
namespace EMPTRACK.Controllers;

[Authorize]
public class AnnouncementsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public AnnouncementsController(ApplicationDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var announcements = await _context.Announcements
            .AsNoTracking()
            .OrderByDescending(a => a.PublishedAt)
            .ThenByDescending(a => a.AnnouncementId)
            .ToListAsync();
        return View(announcements);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue) return NotFound();
        var announcement = await _context.Announcements.AsNoTracking()
            .SingleOrDefaultAsync(a => a.AnnouncementId == id.Value);
        return announcement == null ? NotFound() : View(announcement);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,HR,HR Head")]
    public IActionResult Create() => View(new AnnouncementCreateViewModel());

    [HttpPost]
    [Authorize(Roles = "Admin,HR,HR Head")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnnouncementCreateViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title)) ModelState.AddModelError(nameof(model.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(model.Content)) ModelState.AddModelError(nameof(model.Content), "Content is required.");
        if (!ModelState.IsValid) return View(model);

        var announcement = new Announcement
        {
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            PublishedAt = DateTime.Now
        };
        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "CREATE", "Announcement", $"Announcement \"{announcement.Title}\" was created.");
        TempData["SuccessMessage"] = "Announcement created successfully.";
        return RedirectToAction(nameof(Details), new { id = announcement.AnnouncementId });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,HR,HR Head")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue) return NotFound();
        var announcement = await _context.Announcements.AsNoTracking()
            .SingleOrDefaultAsync(a => a.AnnouncementId == id.Value);
        if (announcement == null) return NotFound();

        return View(new AnnouncementEditViewModel
        {
            AnnouncementId = announcement.AnnouncementId,
            Title = announcement.Title,
            Content = announcement.Content
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR,HR Head")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AnnouncementEditViewModel model)
    {
        if (id != model.AnnouncementId) return NotFound();
        if (string.IsNullOrWhiteSpace(model.Title)) ModelState.AddModelError(nameof(model.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(model.Content)) ModelState.AddModelError(nameof(model.Content), "Content is required.");
        if (!ModelState.IsValid) return View(model);

        var announcement = await _context.Announcements.SingleOrDefaultAsync(a => a.AnnouncementId == id);
        if (announcement == null) return NotFound();
        announcement.Title = model.Title.Trim();
        announcement.Content = model.Content.Trim();
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "UPDATE", "Announcement", $"Announcement \"{announcement.Title}\" was updated.");
        TempData["SuccessMessage"] = "Announcement updated successfully.";
        return RedirectToAction(nameof(Details), new { id = announcement.AnnouncementId });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,HR,HR Head")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue) return NotFound();
        var announcement = await _context.Announcements.AsNoTracking()
            .SingleOrDefaultAsync(a => a.AnnouncementId == id.Value);
        return announcement == null ? NotFound() : View(announcement);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR,HR Head")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var announcement = await _context.Announcements.SingleOrDefaultAsync(a => a.AnnouncementId == id);
        if (announcement == null) return NotFound();
        _context.Announcements.Remove(announcement);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "DELETE", "Announcement", $"Announcement \"{announcement.Title}\" was deleted.");
        TempData["SuccessMessage"] = "Announcement deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
    private int GetCurrentUserAccountIdForAudit()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim == null || !int.TryParse(claim.Value, out var userAccountId))
        {
            throw new InvalidOperationException("The authenticated UserAccountId could not be determined.");
        }

        return userAccountId;
    }

}
