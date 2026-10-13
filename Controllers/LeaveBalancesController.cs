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
public class LeaveBalancesController : Controller
{
    private readonly ApplicationDbContext _context;

    public LeaveBalancesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /LeaveBalances
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpGet]
    public async Task<IActionResult> Index(int? employeeId, int? leaveTypeId)
    {
        var query = _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(lb => lb.EmployeeId == employeeId.Value);
        }

        if (leaveTypeId.HasValue)
        {
            query = query.Where(lb => lb.LeaveTypeId == leaveTypeId.Value);
        }

        ViewData["EmployeeId"] = employeeId;
        ViewData["LeaveTypeId"] = leaveTypeId;

        await PopulateEmployeeFilterAsync(employeeId);
        await PopulateLeaveTypeFilterAsync(leaveTypeId);

        var balances = await query
            .OrderBy(lb => lb.Employee.EmployeeCode)
            .ThenBy(lb => lb.LeaveType.Name)
            .ToListAsync();

        return View(balances);
    }

    // GET: /LeaveBalances/Details/5
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var balance = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .AsNoTracking()
            .SingleOrDefaultAsync(lb => lb.LeaveBalanceId == id.Value);

        if (balance == null)
        {
            return NotFound();
        }

        return View(balance);
    }

    // GET: /LeaveBalances/Create
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateEmployeeListAsync();
        await PopulateLeaveTypeListAsync();

        return View(new LeaveBalanceCreateViewModel());
    }

    // POST: /LeaveBalances/Create
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveBalanceCreateViewModel model)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.EmployeeId == model.EmployeeId);

        if (!employeeExists)
        {
            ModelState.AddModelError(
                nameof(model.EmployeeId),
                "The selected employee does not exist.");
        }

        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.LeaveTypeId == model.LeaveTypeId);

        if (leaveType == null)
        {
            ModelState.AddModelError(
                nameof(model.LeaveTypeId),
                "The selected leave type does not exist.");
        }
        else if (!leaveType.IsActive)
        {
            ModelState.AddModelError(
                nameof(model.LeaveTypeId),
                "An inactive leave type cannot be assigned a new balance.");
        }

        var duplicateExists = await _context.LeaveBalances
            .AnyAsync(lb =>
                lb.EmployeeId == model.EmployeeId &&
                lb.LeaveTypeId == model.LeaveTypeId);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                string.Empty,
                "This employee already has a balance for this leave type.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateEmployeeListAsync(model.EmployeeId);
            await PopulateLeaveTypeListAsync(model.LeaveTypeId);
            return View(model);
        }

        var balance = new LeaveBalance
        {
            EmployeeId = model.EmployeeId,
            LeaveTypeId = model.LeaveTypeId,
            AvailableLeaves = leaveType!.AllocationLimit
        };

        _context.LeaveBalances.Add(balance);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The leave balance could not be saved. " +
                "A balance for this employee and leave type may already exist.");

            await PopulateEmployeeListAsync(model.EmployeeId);
            await PopulateLeaveTypeListAsync(model.LeaveTypeId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Leave balance created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /LeaveBalances/Edit/5
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var balance = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .AsNoTracking()
            .SingleOrDefaultAsync(lb => lb.LeaveBalanceId == id.Value);

        if (balance == null)
        {
            return NotFound();
        }

        var model = new LeaveBalanceEditViewModel
        {
            LeaveBalanceId = balance.LeaveBalanceId,
            EmployeeDisplayName = $"{balance.Employee.EmployeeCode} - {balance.Employee.FirstName} {balance.Employee.LastName}",
            LeaveTypeDisplayName = balance.LeaveType.Name,
            AvailableLeaves = balance.AvailableLeaves
        };

        return View(model);
    }

    // POST: /LeaveBalances/Edit/5
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        LeaveBalanceEditViewModel model)
    {
        if (id != model.LeaveBalanceId)
        {
            return BadRequest();
        }

        var balance = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .SingleOrDefaultAsync(lb => lb.LeaveBalanceId == id);

        if (balance == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.EmployeeDisplayName =
                $"{balance.Employee.EmployeeCode} - {balance.Employee.FirstName} {balance.Employee.LastName}";
            model.LeaveTypeDisplayName = balance.LeaveType.Name;
            return View(model);
        }

        // EmployeeId and LeaveTypeId identify the balance and are never changed here.
        // This edit only changes AvailableLeaves.
        balance.AvailableLeaves = model.AvailableLeaves;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The leave balance could not be updated.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Leave balance updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /LeaveBalances/MyBalances
    // Employees can view their own balances, but cannot change them.
    [HttpGet]
    public async Task<IActionResult> MyBalances()
    {
        var employeeIdValue = User.FindFirst("EmployeeId")?.Value;

        if (!int.TryParse(employeeIdValue, out var employeeId))
        {
            return Forbid();
        }

        var balances = await _context.LeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == employeeId)
            .AsNoTracking()
            .OrderBy(lb => lb.LeaveType.Name)
            .ToListAsync();

        return View(balances);
    }

    private async Task PopulateEmployeeListAsync(int? selectedEmployeeId = null)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmploymentStatus == "Active")
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();

        ViewBag.Employees = employees
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = $"{e.EmployeeCode} - {e.FirstName} {e.LastName}",
                Selected = selectedEmployeeId == e.EmployeeId
            })
            .ToList();
    }

    private async Task PopulateLeaveTypeListAsync(int? selectedLeaveTypeId = null)
    {
        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync();

        ViewBag.LeaveTypes = leaveTypes
            .Select(l => new SelectListItem
            {
                Value = l.LeaveTypeId.ToString(),
                Text = l.Name,
                Selected = selectedLeaveTypeId == l.LeaveTypeId
            })
            .ToList();
    }

    private async Task PopulateEmployeeFilterAsync(int? selectedEmployeeId = null)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();

        ViewBag.EmployeeFilter = employees
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = $"{e.EmployeeCode} - {e.FirstName} {e.LastName}",
                Selected = selectedEmployeeId == e.EmployeeId
            })
            .ToList();
    }

    private async Task PopulateLeaveTypeFilterAsync(int? selectedLeaveTypeId = null)
    {
        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .ToListAsync();

        ViewBag.LeaveTypeFilter = leaveTypes
            .Select(l => new SelectListItem
            {
                Value = l.LeaveTypeId.ToString(),
                Text = l.IsActive ? l.Name : $"{l.Name} (Inactive)",
                Selected = selectedLeaveTypeId == l.LeaveTypeId
            })
            .ToList();
    }
}
