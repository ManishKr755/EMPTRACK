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
        var currentEmployeeId = int.TryParse(User.FindFirst("EmployeeId")?.Value, out var parsedEmployeeId) ? parsedEmployeeId : (int?)null;
        if (!User.IsInRole("Admin"))
        {
            var current = currentEmployeeId.HasValue ? await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentEmployeeId.Value) : null;
            if (current == null) return Forbid();
            if (current.Role == "HR Head")
            {
                var scopedDepartmentIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                employeesQuery = employeesQuery.Where(e => e.DepartmentId.HasValue && scopedDepartmentIds.Contains(e.DepartmentId.Value));
            }
            else
            {
                if (!current.DepartmentId.HasValue) return Forbid();
                var scopedDepartmentId = current.DepartmentId.Value;
                employeesQuery = employeesQuery.Where(e => e.DepartmentId == scopedDepartmentId);
            }
        }
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
        if (!User.IsInRole("Admin"))
        {
            var currentId = int.TryParse(User.FindFirst("EmployeeId")?.Value, out var parsedId) ? parsedId : 0;
            var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId);
            if (current == null) return NotFound();
            var inScope = current.DepartmentId.HasValue && current.DepartmentId == employee.DepartmentId;
            if (!inScope && current.Role == "HR Head" && employee.DepartmentId.HasValue)
                inScope = await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == employee.DepartmentId.Value && d.HeadHREmployeeId == current.EmployeeId);
            if (!inScope) return NotFound();
        }

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
    public IActionResult Create() => Forbid(); // Balances are service/accrual/approval-managed.

    // POST: /LeaveBalances/Create
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(LeaveBalanceCreateViewModel model) => Forbid(); // Balances are service/accrual/approval-managed.

    // GET: /LeaveBalances/Edit/5
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpGet]
    public IActionResult Edit(int? id) => Forbid(); // Balances are service/accrual/approval-managed.

    // POST: /LeaveBalances/Edit/5
    [Authorize(Roles = "Admin,HR Head,HR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, LeaveBalanceEditViewModel model) => Forbid(); // Balances are service/accrual/approval-managed.

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
        var employeesQuery = _context.Employees.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin"))
        {
            var currentId = int.TryParse(User.FindFirst("EmployeeId")?.Value, out var parsedId) ? parsedId : 0;
            var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId);
            if (current == null) { ViewBag.EmployeeFilter = new List<SelectListItem>(); return; }
            if (current.Role == "HR Head")
            {
                var deptIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                employeesQuery = employeesQuery.Where(e => e.DepartmentId.HasValue && deptIds.Contains(e.DepartmentId.Value));
            }
            else employeesQuery = current.DepartmentId.HasValue ? employeesQuery.Where(e => e.DepartmentId == current.DepartmentId.Value) : employeesQuery.Where(e => false);
        }
        var employees = await employeesQuery.OrderBy(e => e.EmployeeCode).ToListAsync();

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
