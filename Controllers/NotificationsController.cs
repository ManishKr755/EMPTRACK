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

        // Admin is not an Employee. Management users may view the
        // notification records without being assigned an employee.
        if (IsManagementUser())
        {
            var notifications = await _context.Notifications
                .Include(n => n.Employee)
                .AsNoTracking()
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

        if (!currentEmployeeId.HasValue && IsManagementUser())
        {
            return View(notification);
        }

        return Forbid();
    }

    // GET: /Notifications/Create
    [HttpGet]
    [Authorize(Roles = "Admin,HR Head,HR,Head Manager,Manager")]
    public async Task<IActionResult> Create()
    {
        await PopulateEmployeeListAsync();
        return View();
    }

    // POST: /Notifications/Create
    [HttpPost]
    [Authorize(Roles = "Admin,HR Head,HR,Head Manager,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        NotificationCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEmployeeListAsync(model.EmployeeId);
            return View(model);
        }

        try
        {
            var notification =
                await _notificationService.CreateNotificationAsync(
                    model.EmployeeId,
                    model.Title,
                    model.Message);

            TempData["SuccessMessage"] =
                "Notification created successfully.";

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
        var employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();

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
