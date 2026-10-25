using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using EMPTRACK.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

namespace EMPTRACK.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;

    public NotificationsController(
        ApplicationDbContext context,
        NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // GET: /Notifications
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employeeId = GetCurrentEmployeeId();

        if (employeeId.HasValue)
        {
            var notifications = await _context.Notifications
                .Include(n => n.Employee)
                .AsNoTracking()
                .Where(n => n.EmployeeId == employeeId.Value)
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.NotificationId)
                .ToListAsync();

            return View(notifications);
        }

        return Forbid();
    }

    // GET: /Notifications/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var notification = await _context.Notifications
            .Include(n => n.Employee)
            .AsNoTracking()
            .SingleOrDefaultAsync(n => n.NotificationId == id.Value);

        if (notification == null)
        {
            return NotFound();
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        if (currentEmployeeId.HasValue &&
            notification.EmployeeId == currentEmployeeId.Value)
        {
            return View(notification);
        }

        return NotFound();
    }

    // GET: /Notifications/Create
    [HttpGet]
    [Authorize(Roles = "Admin,HR Head,Head Manager,Manager")]
    public async Task<IActionResult> Create()
    {
        await PopulateEmployeeListAsync();
        return View();
    }

    // POST: /Notifications/Create
    [HttpPost]
    [Authorize(Roles = "Admin,HR Head,Head Manager,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        NotificationCreateViewModel model)
    {
        if (model.SendToScope) ModelState.Remove(nameof(model.EmployeeId));
        if (!ModelState.IsValid || (!model.SendToScope && !await CanNotifyEmployeeAsync(model.EmployeeId)))
        {
            if (ModelState.IsValid && !model.SendToScope) ModelState.AddModelError(nameof(model.EmployeeId), "You cannot send notifications to that employee.");
            await PopulateEmployeeListAsync(model.EmployeeId);
            return View(model);
        }

        try
        {
            List<int> recipientIds;
            if (model.SendToScope)
            {
                recipientIds = await GetAuthorizedRecipientIdsAsync();
                if (recipientIds.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "No active recipients were found in your authorized scope.");
                    await PopulateEmployeeListAsync(model.EmployeeId);
                    return View(model);
                }
            }
            else recipientIds = new List<int> { model.EmployeeId };

            var created = await _notificationService.CreateNotificationsAsync(recipientIds, model.Title, model.Message);
            TempData["SuccessMessage"] = $"Notification delivered to {created.Count} recipient(s). Each recipient has an independent read status.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                ex.ParamName ?? string.Empty,
                ex.Message);

            await PopulateEmployeeListAsync(model.EmployeeId);
            return View(model);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The notification could not be saved.");

            await PopulateEmployeeListAsync(model.EmployeeId);
            return View(model);
        }
    }

    // POST: /Notifications/MarkAsRead/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var employeeId = GetCurrentEmployeeId();

        // Admin has no EmployeeId and therefore cannot mark an employee's
        // notification as read.
        if (!employeeId.HasValue)
        {
            return Forbid();
        }

        var notification = await _context.Notifications
            .SingleOrDefaultAsync(n =>
                n.NotificationId == id &&
                n.EmployeeId == employeeId.Value);

        if (notification == null)
        {
            return NotFound();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] =
            "Notification marked as read.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Notifications/MarkAllAsRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var employeeId = GetCurrentEmployeeId();

        if (!employeeId.HasValue)
        {
            return Forbid();
        }

        var unreadNotifications = await _context.Notifications
            .Where(n =>
                n.EmployeeId == employeeId.Value &&
                !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        if (unreadNotifications.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] =
            unreadNotifications.Count == 0
                ? "There are no unread notifications."
                : "All notifications marked as read.";

        return RedirectToAction(nameof(Index));
    }


    private async Task<List<int>> GetAuthorizedRecipientIdsAsync()
    {
        var active = _context.Employees.AsNoTracking().Where(e => e.EmploymentStatus == "Active");
        if (User.IsInRole("Admin")) return await active.Select(e => e.EmployeeId).ToListAsync();
        var currentId = GetCurrentEmployeeId();
        if (!currentId.HasValue) return new List<int>();
        var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value);
        if (current == null) return new List<int>();
        if (current.Role == "Manager" && current.TeamId.HasValue)
            return await active.Where(e => e.TeamId == current.TeamId.Value).Select(e => e.EmployeeId).ToListAsync();
        if (current.Role == "Head Manager" && current.DepartmentId.HasValue)
            return await active.Where(e => e.DepartmentId == current.DepartmentId.Value).Select(e => e.EmployeeId).ToListAsync();
        if (current.Role == "HR Head")
        {
            var departments = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
            return await active.Where(e => e.DepartmentId.HasValue && departments.Contains(e.DepartmentId.Value)).Select(e => e.EmployeeId).ToListAsync();
        }
        return new List<int>();
    }

    private async Task<bool> CanNotifyEmployeeAsync(int recipientId)
    {
        var recipient = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == recipientId && e.EmploymentStatus == "Active");
        if (recipient == null) return false;
        if (User.IsInRole("Admin")) return true;
        var currentId = GetCurrentEmployeeId();
        if (!currentId.HasValue) return false;
        var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value);
        if (current == null) return false;
        if (current.Role == "Manager") return current.TeamId.HasValue && recipient.TeamId == current.TeamId;
        if (current.Role == "Head Manager") return current.DepartmentId.HasValue && recipient.DepartmentId == current.DepartmentId;
        if (current.Role == "HR Head") return recipient.DepartmentId == current.DepartmentId || await _context.Departments.AnyAsync(d => d.DepartmentId == recipient.DepartmentId && d.HeadHREmployeeId == current.EmployeeId);
        return false;
    }

    private int? GetCurrentEmployeeId()
    {
        var claimValue = User.FindFirstValue("EmployeeId");

        if (int.TryParse(claimValue, out var employeeId) &&
            employeeId > 0)
        {
            return employeeId;
        }

        return null;
    }

    private bool IsManagementUser()
    {
        return User.IsInRole("Admin") ||
               User.IsInRole("HR Head") ||
               User.IsInRole("HR") ||
               User.IsInRole("Head Manager") ||
               User.IsInRole("Manager");
    }

    private async Task PopulateEmployeeListAsync(
        int? selectedEmployeeId = null)
    {
        var currentEmployeeId = GetCurrentEmployeeId();
        var current = currentEmployeeId.HasValue ? await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentEmployeeId.Value) : null;
        var query = _context.Employees.AsNoTracking().Where(e => e.EmploymentStatus == "Active");
        if (!User.IsInRole("Admin"))
        {
            if (current == null) query = query.Where(e => false);
            else if (current.Role == "Manager" && current.TeamId.HasValue) query = query.Where(e => e.TeamId == current.TeamId.Value);
            else if (current.Role == "Head Manager" && current.DepartmentId.HasValue) query = query.Where(e => e.DepartmentId == current.DepartmentId.Value);
            else if (current.Role == "HR Head")
        {
            var departmentIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
            query = query.Where(e => e.DepartmentId.HasValue && departmentIds.Contains(e.DepartmentId.Value));
        }
            else query = query.Where(e => false);
        }
        var employees = await query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync();

        ViewBag.Employees = employees
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text =
                    $"{e.EmployeeCode} - {e.FirstName} {e.LastName}",
                Selected = selectedEmployeeId == e.EmployeeId
            })
            .ToList();
    }
}
