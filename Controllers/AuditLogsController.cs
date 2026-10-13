using EMPTRACK.Data;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace EMPTRACK.Controllers;

[Authorize(Roles = "Admin,HR Head")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /AuditLogs
    public async Task<IActionResult> Index(
        string? search,
        string? action,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = _context.AuditLogs
            .Include(a => a.UserAccount)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(a =>
                a.Action.Contains(search) ||
                a.Target.Contains(search) ||
                a.Description.Contains(search) ||
                a.UserAccount.Username.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            action = action.Trim();

            query = query.Where(a =>
                a.Action == action);
        }

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;

            query = query.Where(a =>
                a.DateTime >= from);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);

            query = query.Where(a =>
                a.DateTime < toExclusive);
        }

        var logs = await query
            .OrderByDescending(a => a.DateTime)
            .Select(a => new AuditLogListItemViewModel
            {
                AuditLogId = a.AuditLogId,
                UserAccountId = a.UserAccountId,
                Username = a.UserAccount.Username,
                Action = a.Action,
                Target = a.Target,
                DateTime = a.DateTime,
                Description = a.Description
            })
            .ToListAsync();

        var model = new AuditLogIndexViewModel
        {
            Logs = logs,
            Search = search,
            Action = action,
            FromDate = fromDate,
            ToDate = toDate
        };

        ViewBag.Actions = await _context.AuditLogs
            .Select(a => a.Action)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return View(model);
    }

    // GET: /AuditLogs/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var auditLog = await _context.AuditLogs
            .Include(a => a.UserAccount)
            .SingleOrDefaultAsync(a =>
                a.AuditLogId == id.Value);

        if (auditLog == null)
        {
            return NotFound();
        }

        return View(auditLog);
    }
}