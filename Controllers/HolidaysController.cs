using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using EMPTRACK.Services;
namespace EMPTRACK.Controllers;

[Authorize]
public class HolidaysController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public HolidaysController(ApplicationDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    // GET: /Holidays
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var holidays = await _context.Holidays
            .AsNoTracking()
            .OrderBy(h => h.Date)
            .ThenBy(h => h.Name)
            .ToListAsync();

        return View(holidays);
    }

    // GET: /Holidays/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var holiday = await _context.Holidays
            .AsNoTracking()
            .SingleOrDefaultAsync(h => h.HolidayId == id.Value);

        if (holiday == null)
        {
            return NotFound();
        }

        return View(holiday);
    }

    // GET: /Holidays/Create
    [HttpGet]
    [Authorize(Roles = "Admin,HR Head,HR")]
    public IActionResult Create()
    {
        return View(new HolidayCreateViewModel
        {
            Date = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    // POST: /Holidays/Create
    [HttpPost]
    [Authorize(Roles = "Admin,HR Head,HR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HolidayCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var holiday = new Holiday
        {
            Name = model.Name.Trim(),
            Date = model.Date
        };

        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "CREATE", "Holiday", $"Holiday \"{holiday.Name}\" was created.");

        TempData["SuccessMessage"] = "Holiday created successfully.";

        return RedirectToAction(nameof(Details), new { id = holiday.HolidayId });
    }

    // GET: /Holidays/Edit/5
    [HttpGet]
    [Authorize(Roles = "Admin,HR Head,HR")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var holiday = await _context.Holidays
            .AsNoTracking()
            .SingleOrDefaultAsync(h => h.HolidayId == id.Value);

        if (holiday == null)
        {
            return NotFound();
        }

        return View(new HolidayEditViewModel
        {
            HolidayId = holiday.HolidayId,
            Name = holiday.Name,
            Date = holiday.Date
        });
    }

    // POST: /Holidays/Edit/5
    [HttpPost]
    [Authorize(Roles = "Admin,HR Head,HR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        HolidayEditViewModel model)
    {
        if (id != model.HolidayId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var holiday = await _context.Holidays
            .SingleOrDefaultAsync(h => h.HolidayId == id);

        if (holiday == null)
        {
            return NotFound();
        }

        holiday.Name = model.Name.Trim();
        holiday.Date = model.Date;

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "UPDATE", "Holiday", $"Holiday \"{holiday.Name}\" was updated.");

        TempData["SuccessMessage"] = "Holiday updated successfully.";

        return RedirectToAction(nameof(Details), new { id = holiday.HolidayId });
    }

    // GET: /Holidays/Delete/5
    [HttpGet]
    [Authorize(Roles = "Admin,HR Head,HR")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var holiday = await _context.Holidays
            .AsNoTracking()
            .SingleOrDefaultAsync(h => h.HolidayId == id.Value);

        if (holiday == null)
        {
            return NotFound();
        }

        return View(holiday);
    }

    // POST: /Holidays/Delete/5
    [HttpPost]
    [Authorize(Roles = "Admin,HR Head,HR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var holiday = await _context.Holidays
            .SingleOrDefaultAsync(h => h.HolidayId == id);

        if (holiday == null)
        {
            return NotFound();
        }

        _context.Holidays.Remove(holiday);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "DELETE", "Holiday", $"Holiday \"{holiday.Name}\" was deleted.");

        TempData["SuccessMessage"] = "Holiday deleted successfully.";

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
