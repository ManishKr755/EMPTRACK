using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using EMPTRACK.Services;
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
    private readonly LeaveBalanceService _leaveBalanceService;

    public LeaveBalancesController(ApplicationDbContext context, LeaveBalanceService leaveBalanceService)
    {
        _context = context;
        _leaveBalanceService = leaveBalanceService;
    }

    // GET: /LeaveBalances - one row per employee with aggregated balances.
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpGet]
    public async Task<IActionResult> Index(int? employeeId, int? leaveTypeId)
    {
        await _leaveBalanceService.EnsureAndAccrueAsync();

        var employeesQuery = _context.Employees.AsNoTracking().AsQueryable();
        if (employeeId.HasValue)
            employeesQuery = employeesQuery.Where(e => e.EmployeeId == employeeId.Value);

        var employees = await employeesQuery
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new { e.EmployeeId, e.EmployeeCode, e.FirstName, e.LastName })
            .ToListAsync();

        var balancesQuery = _context.LeaveBalances
            .Include(b => b.LeaveType)
            .AsNoTracking()
            .AsQueryable();
        if (employeeId.HasValue)
            balancesQuery = balancesQuery.Where(b => b.EmployeeId == employeeId.Value);
        if (leaveTypeId.HasValue)
            balancesQuery = balancesQuery.Where(b => b.LeaveTypeId == leaveTypeId.Value);

        var balances = await balancesQuery.ToListAsync();
        var balanceGroups = balances.GroupBy(b => b.EmployeeId)
            .ToDictionary(g => g.Key, g => new
            {
                Allocated = g.Sum(b => b.LeaveType.AllocationLimit),
                Available = g.Sum(b => b.AvailableLeaves)
            });

        // Include employees even when they currently have no balance records.
        var rows = employees.Select(e =>
        {
            balanceGroups.TryGetValue(e.EmployeeId, out var totals);
            return new EmployeeLeaveBalanceSummaryViewModel
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                EmployeeName = $"{e.FirstName} {e.LastName}",
                TotalAllocated = totals?.Allocated ?? 0,
                TotalAvailable = totals?.Available ?? 0
            };
        }).ToList();

        ViewData["EmployeeId"] = employeeId;
        ViewData["LeaveTypeId"] = leaveTypeId;
        await PopulateEmployeeFilterAsync(employeeId);
        await PopulateLeaveTypeFilterAsync(leaveTypeId);
        return View(rows);
    }

    // GET: /LeaveBalances/Details/5 - all leave types for the selected employee.
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue) return NotFound();

        await _leaveBalanceService.EnsureAndAccrueAsync();
        var employee = await _context.Employees.AsNoTracking()
            .SingleOrDefaultAsync(e => e.EmployeeId == id.Value);
        if (employee == null) return NotFound();

        var leaveTypes = await _context.LeaveTypes.AsNoTracking()
            .OrderBy(t => t.Name).ToListAsync();
        var balances = await _context.LeaveBalances.AsNoTracking()
            .Where(b => b.EmployeeId == id.Value).ToListAsync();
        var balanceByType = balances.ToDictionary(b => b.LeaveTypeId);

        var rows = leaveTypes.Select(type =>
        {
            balanceByType.TryGetValue(type.LeaveTypeId, out var balance);
            var allocated = type.AllocationLimit;
            var available = balance?.AvailableLeaves ?? 0;
            return new LeaveTypeBalanceRowViewModel
            {
                LeaveTypeId = type.LeaveTypeId,
                Name = type.Name,
                AllocationType = type.AllocationType,
                Allocated = allocated,
                Available = available,
                Used = Math.Max(0, allocated - available),
                IsActive = type.IsActive
            };
        }).ToList();

        return View(new EmployeeLeaveBalanceDetailViewModel
        {
            EmployeeId = employee.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            TotalAllocated = rows.Sum(r => r.Allocated),
            TotalAvailable = rows.Sum(r => r.Available),
            LeaveTypes = rows
        });
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
