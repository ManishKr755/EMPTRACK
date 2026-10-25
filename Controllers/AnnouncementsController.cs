using System.Security.Claims;
using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace EMPTRACK.Controllers;

[Authorize]
public class AnnouncementsController : Controller
{
    private readonly ApplicationDbContext _context;
    public AnnouncementsController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var query = _context.Announcements.Include(a => a.Department).AsNoTracking();
        if (!User.IsInRole("Admin"))
        {
            var departmentId = await CurrentDepartmentIdAsync();
            query = query.Where(a => a.AudienceType == "AllExceptAdmin" || (a.AudienceType == "Department" && departmentId.HasValue && a.DepartmentId == departmentId.Value));
        }
        var items = await query.OrderByDescending(a => a.PublishedAt).ThenByDescending(a => a.AnnouncementId).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue) return NotFound();
        var a = await _context.Announcements.Include(x => x.Department).AsNoTracking().SingleOrDefaultAsync(x => x.AnnouncementId == id.Value);
        if (a == null) return NotFound();
        if (!await CanReadAsync(a)) return NotFound();
        return View(a);
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create() { await LoadDepartments(); return View(new AnnouncementCreateViewModel()); }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnnouncementCreateViewModel model)
    {
        await ValidateAudience(model.AudienceType, model.DepartmentId);
        if (!ModelState.IsValid) { await LoadDepartments(model.DepartmentId); return View(model); }
        var a = new Announcement { Title = model.Title.Trim(), Content = model.Content.Trim(), PublishedAt = DateTime.Now, AudienceType = model.AudienceType, DepartmentId = model.AudienceType == "Department" ? model.DepartmentId : null };
        _context.Announcements.Add(a); await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = a.AnnouncementId });
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue) return NotFound();
        var a = await _context.Announcements.AsNoTracking().SingleOrDefaultAsync(x => x.AnnouncementId == id.Value);
        if (a == null) return NotFound();
        await LoadDepartments(a.DepartmentId);
        return View(new AnnouncementEditViewModel { AnnouncementId = a.AnnouncementId, Title = a.Title, Content = a.Content, AudienceType = a.AudienceType, DepartmentId = a.DepartmentId });
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AnnouncementEditViewModel model)
    {
        if (id != model.AnnouncementId) return NotFound();
        await ValidateAudience(model.AudienceType, model.DepartmentId);
        if (!ModelState.IsValid) { await LoadDepartments(model.DepartmentId); return View(model); }
        var a = await _context.Announcements.SingleOrDefaultAsync(x => x.AnnouncementId == id);
        if (a == null) return NotFound();
        a.Title = model.Title.Trim(); a.Content = model.Content.Trim(); a.AudienceType = model.AudienceType; a.DepartmentId = model.AudienceType == "Department" ? model.DepartmentId : null;
        await _context.SaveChangesAsync(); return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue) return NotFound();
        var a = await _context.Announcements.Include(x => x.Department).AsNoTracking().SingleOrDefaultAsync(x => x.AnnouncementId == id.Value);
        return a == null ? NotFound() : View(a);
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _context.Announcements.SingleOrDefaultAsync(x => x.AnnouncementId == id);
        if (a == null) return NotFound();
        _context.Announcements.Remove(a); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }

    private async Task<int?> CurrentDepartmentIdAsync()
    {
        var raw = User.FindFirstValue("EmployeeId");
        if (!int.TryParse(raw, out var employeeId)) return null;
        return await _context.Employees.AsNoTracking().Where(e => e.EmployeeId == employeeId).Select(e => e.DepartmentId).FirstOrDefaultAsync();
    }

    private async Task<bool> CanReadAsync(Announcement a)
    {
        if (User.IsInRole("Admin") || a.AudienceType == "AllExceptAdmin") return true;
        var departmentId = await CurrentDepartmentIdAsync();
        return a.AudienceType == "Department" && a.DepartmentId.HasValue && a.DepartmentId == departmentId;
    }

    private async Task ValidateAudience(string audienceType, int? departmentId)
    {
        if (audienceType == "Department")
        {
            if (!departmentId.HasValue || !await _context.Departments.AnyAsync(d => d.DepartmentId == departmentId.Value)) ModelState.AddModelError(nameof(AnnouncementCreateViewModel.DepartmentId), "Select a valid department.");
        }
        else if (audienceType != "AllExceptAdmin") ModelState.AddModelError(nameof(AnnouncementCreateViewModel.AudienceType), "Select a valid audience.");
    }

    private async Task LoadDepartments(int? selected = null)
    {
        ViewBag.Departments = await _context.Departments.AsNoTracking().OrderBy(d => d.Name).Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.Name, Selected = selected == d.DepartmentId }).ToListAsync();
    }
}
