using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using EMPTRACK.Services;
namespace EMPTRACK.Controllers;

[Authorize]
public class LeaveRequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly LeaveBalanceService _leaveBalanceService;

    private static readonly string[] RequestViewerRoles =
    {
        "Admin",
        "HR Head",
        "HR",
        "Head Manager",
        "Manager"
    };

    public LeaveRequestsController(ApplicationDbContext context, LeaveBalanceService leaveBalanceService)
    {
        _context = context;
        _leaveBalanceService = leaveBalanceService;
    }

    // GET: /LeaveRequests
    [HttpGet]
    public async Task<IActionResult> Index(
        int? employeeId,
        int? leaveTypeId,
        string? status,
        DateOnly? startDate,
        DateOnly? endDate)
    {
        var currentUser = await GetCurrentUserAccountAsync();

        if (currentUser == null)
        {
            return Forbid();
        }

        var canViewAllRequests = IsRequestViewer();

        var query = _context.LeaveRequests
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Team)
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Department)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.ApprovedByUser)
            .AsNoTracking()
            .AsQueryable();

        if (!canViewAllRequests)
        {
            if (!currentUser.EmployeeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(lr =>
                lr.EmployeeId == currentUser.EmployeeId.Value);

            // Ignore an employeeId supplied by a normal employee.
            employeeId = currentUser.EmployeeId;
        }
        else if (employeeId.HasValue)
        {
            query = query.Where(lr =>
                lr.EmployeeId == employeeId.Value);
        }

        if (leaveTypeId.HasValue)
        {
            query = query.Where(lr =>
                lr.LeaveTypeId == leaveTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var validStatus =
                status is "Pending" or "Approved" or "Rejected";

            if (validStatus)
            {
                query = query.Where(lr => lr.Status == status);
            }
            else
            {
                status = null;
            }
        }

        if (startDate.HasValue)
        {
            query = query.Where(lr =>
                lr.EndDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(lr =>
                lr.StartDate <= endDate.Value);
        }

        // When an approval user asks specifically for Pending requests,
        // show only requests that this user can actually act on.
        if (status == "Pending" && IsApprovalUser())
        {
            var pendingRequests = await query.ToListAsync();

            pendingRequests = pendingRequests
                .Where(r => CanUserActOnRequest(currentUser, r))
                .ToList();

            ViewData["EmployeeId"] = employeeId;
            ViewData["LeaveTypeId"] = leaveTypeId;
            ViewData["Status"] = status;
            ViewData["StartDate"] =
                startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] =
                endDate?.ToString("yyyy-MM-dd");

            ViewBag.CanViewAllRequests = canViewAllRequests;
            ViewBag.CanCreateRequest =
                currentUser.EmployeeId.HasValue &&
                currentUser.Employee != null &&
                string.Equals(
                    currentUser.Employee.EmploymentStatus,
                    "Active",
                    StringComparison.OrdinalIgnoreCase);

            if (canViewAllRequests)
            {
                await PopulateEmployeeFilterAsync(employeeId);
            }

            await PopulateLeaveTypeFilterAsync(leaveTypeId);

            var approvablePendingIds = pendingRequests
                .Select(r => r.LeaveRequestId)
                .ToHashSet();

            ViewBag.CanApproveRequests =
                approvablePendingIds.Count > 0;
            ViewBag.ApprovableRequestIds =
                approvablePendingIds;

            return View(
                pendingRequests
                    .OrderByDescending(r => r.StartDate)
                    .ThenByDescending(r => r.LeaveRequestId)
                    .ToList());
        }

        ViewData["EmployeeId"] = employeeId;
        ViewData["LeaveTypeId"] = leaveTypeId;
        ViewData["Status"] = status;
        ViewData["StartDate"] =
            startDate?.ToString("yyyy-MM-dd");
        ViewData["EndDate"] =
            endDate?.ToString("yyyy-MM-dd");

        ViewBag.CanViewAllRequests = canViewAllRequests;
        ViewBag.CanCreateRequest =
            currentUser.EmployeeId.HasValue &&
            currentUser.Employee != null &&
            string.Equals(
                currentUser.Employee.EmploymentStatus,
                "Active",
                StringComparison.OrdinalIgnoreCase);

        if (canViewAllRequests)
        {
            await PopulateEmployeeFilterAsync(employeeId);
        }

        await PopulateLeaveTypeFilterAsync(leaveTypeId);

        var requests = await query
            .OrderByDescending(lr => lr.StartDate)
            .ThenByDescending(lr => lr.LeaveRequestId)
            .ToListAsync();

        var approvableRequestIds = new HashSet<int>();

        if (IsApprovalUser())
        {
            foreach (var request in requests.Where(r =>
                         r.Status == "Pending"))
            {
                if (CanUserActOnRequest(currentUser, request))
                {
                    approvableRequestIds.Add(
                        request.LeaveRequestId);
                }
            }
        }

        ViewBag.CanApproveRequests = approvableRequestIds.Count > 0;
        ViewBag.ApprovableRequestIds = approvableRequestIds;

        return View(requests);
    }

    // GET: /LeaveRequests/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var currentUser = await GetCurrentUserAccountAsync();

        if (currentUser == null)
        {
            return Forbid();
        }

        var request = await _context.LeaveRequests
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Team)
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Department)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.ApprovedByUser)
            .AsNoTracking()
            .SingleOrDefaultAsync(lr =>
                lr.LeaveRequestId == id.Value);

        if (request == null)
        {
            return NotFound();
        }

        if (!IsRequestViewer())
        {
            if (!currentUser.EmployeeId.HasValue ||
                request.EmployeeId != currentUser.EmployeeId.Value)
            {
                // Do not reveal another employee's request.
                return NotFound();
            }
        }

        ViewBag.CanViewApprover = IsRequestViewer();

        ViewBag.CanApprove =
            request.Status == "Pending" &&
            CanUserActOnRequest(currentUser, request);

        return View(request);
    }

    // POST: /LeaveRequests/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var currentUser = await GetCurrentUserAccountAsync();

        if (currentUser == null)
        {
            return Forbid();
        }

        var request = await _context.LeaveRequests
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Team)
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Department)
            .AsNoTracking()
            .SingleOrDefaultAsync(lr =>
                lr.LeaveRequestId == id);

        if (request == null)
        {
            TempData["ErrorMessage"] =
                "Leave request was not found.";

            return RedirectToAction(nameof(Index));
        }

        if (request.Status != "Pending")
        {
            TempData["ErrorMessage"] =
                "This leave request is no longer pending.";

            return RedirectToAction(nameof(Index));
        }

        if (!CanUserActOnRequest(currentUser, request))
        {
            TempData["ErrorMessage"] =
                "You are not authorized to approve this request.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Ensure a balance exists and current monthly/annual accruals are applied
            // before the stored procedure checks and deducts the requested days.
            await _leaveBalanceService.EnsureAndAccrueAsync();

            // The stored procedure performs the complete approval
            // transaction, including the balance deduction.
            await _context.Database.ExecuteSqlInterpolatedAsync(
    $"EXEC dbo.ApproveLeaveRequest @LeaveRequestId = {id}, @ApprovedByUserId = {currentUser.UserAccountId}");

            TempData["SuccessMessage"] =
                "Leave request approved successfully.";
        }
        catch (SqlException ex)
        {
            TempData["ErrorMessage"] =
                GetApprovalErrorMessage(ex);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] =
                "The leave request could not be approved.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /LeaveRequests/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var currentUser = await GetCurrentUserAccountAsync();

        if (currentUser == null)
        {
            return Forbid();
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var request = await _context.LeaveRequests
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Team)
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Department)
            .SingleOrDefaultAsync(lr =>
                lr.LeaveRequestId == id);

        if (request == null)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "Leave request was not found.";

            return RedirectToAction(nameof(Index));
        }

        if (request.Status != "Pending")
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "This leave request is no longer pending.";

            return RedirectToAction(nameof(Index));
        }

        if (!CanUserActOnRequest(currentUser, request))
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "You are not authorized to reject this request.";

            return RedirectToAction(nameof(Index));
        }

        // Rejection does not change LeaveBalance.
        request.Status = "Rejected";
        request.ApprovedByUserId = null;
        request.ApprovedAt = null;

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Leave request rejected successfully.";
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "This leave request was changed by another user. " +
                "Please refresh and try again.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            TempData["ErrorMessage"] =
                "The leave request could not be rejected.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /LeaveRequests/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var currentUser = await GetCurrentUserAccountAsync();

        if (currentUser?.EmployeeId == null ||
            currentUser.Employee == null)
        {
            // Admin accounts are not employees and cannot create
            // normal employee leave requests.
            return Forbid();
        }

        if (!string.Equals(
                currentUser.Employee.EmploymentStatus,
                "Active",
                StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        await PopulateLeaveTypeListAsync(
            currentUser.EmployeeId.Value);

        ViewBag.CurrentEmployee = currentUser.Employee;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var model = new LeaveRequestCreateViewModel
        {
            StartDate = today,
            EndDate = today
        };

        return View(model);
    }

    // POST: /LeaveRequests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        LeaveRequestCreateViewModel model)
    {
        var currentUser = await GetCurrentUserAccountAsync();

        if (currentUser?.EmployeeId == null ||
            currentUser.Employee == null)
        {
            return Forbid();
        }

        if (!string.Equals(
                currentUser.Employee.EmploymentStatus,
                "Active",
                StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var employeeId = currentUser.EmployeeId.Value;

        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(lt =>
                lt.LeaveTypeId == model.LeaveTypeId);

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
                "The selected leave type is inactive.");
        }

        if (model.EndDate < model.StartDate)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                "End date cannot be earlier than start date.");
        }

        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            ModelState.AddModelError(
                nameof(model.Reason),
                "Please enter a reason for the leave request.");
        }

        if (model.EndDate >= model.StartDate)
        {
            var overlappingRequestExists =
                await _context.LeaveRequests.AnyAsync(lr =>
                    lr.EmployeeId == employeeId &&
                    (lr.Status == "Pending" ||
                     lr.Status == "Approved") &&
                    model.StartDate <= lr.EndDate &&
                    model.EndDate >= lr.StartDate);

            if (overlappingRequestExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You already have a pending or approved leave request that overlaps this period.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateLeaveTypeListAsync(
                employeeId,
                model.LeaveTypeId);

            ViewBag.CurrentEmployee = currentUser.Employee;

            return View(model);
        }

        var leaveRequest = new LeaveRequest
        {
            // EmployeeId is taken from the authenticated UserAccount.
            // It is never accepted from the browser.
            EmployeeId = employeeId,
            LeaveTypeId = model.LeaveTypeId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Reason = model.Reason.Trim(),
            Status = "Pending",
            ApprovedByUserId = null,
            ApprovedAt = null
        };

        _context.LeaveRequests.Add(leaveRequest);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The leave request could not be saved.");

            await PopulateLeaveTypeListAsync(
                employeeId,
                model.LeaveTypeId);

            ViewBag.CurrentEmployee = currentUser.Employee;

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Leave request submitted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private bool IsRequestViewer()
    {
        return RequestViewerRoles.Any(User.IsInRole);
    }

    private bool IsApprovalUser()
    {
        return User.IsInRole("Admin") ||
               User.IsInRole("HR Head") ||
               User.IsInRole("Head Manager") ||
               User.IsInRole("Manager");
    }

    private bool CanUserActOnRequest(
        UserAccount currentUser,
        LeaveRequest request)
    {
        var employee = request.Employee;

        if (employee == null)
        {
            return false;
        }

        // An employee must never approve their own request.
        if (currentUser.EmployeeId.HasValue &&
            currentUser.EmployeeId.Value == request.EmployeeId)
        {
            return false;
        }

        // Admin is an application-level account and has no Employee row.
        if (User.IsInRole("Admin") &&
            currentUser.EmployeeId == null)
        {
            return true;
        }

        if (!currentUser.EmployeeId.HasValue ||
            currentUser.Employee == null)
        {
            return false;
        }

        var currentEmployeeId =
            currentUser.EmployeeId.Value;

        // Head Manager can intervene in requests from their department.
        if (User.IsInRole("Head Manager") &&
            employee.Department?.HeadManagerEmployeeId ==
                currentEmployeeId)
        {
            return true;
        }

        // HR Head is the normal approver for HR employee requests.
        if (User.IsInRole("HR Head") &&
            employee.Department?.HeadHREmployeeId ==
                currentEmployeeId)
        {
            return true;
        }

        // A Head Manager's own request and an HR Head's own request
        // are handled by Admin, not by themselves.
        if (string.Equals(
                employee.Role,
                "Head Manager",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                employee.Role,
                "HR Head",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // HR employees are normally approved by the HR Head.
        if (IsHrDepartment(employee) &&
            employee.Department?.HeadHREmployeeId ==
                currentEmployeeId &&
            User.IsInRole("HR Head"))
        {
            return true;
        }

        // Normal employees are approved by their team's Manager.
        if (User.IsInRole("Manager") &&
            employee.Team?.ManagerEmployeeId ==
                currentEmployeeId)
        {
            return true;
        }

        return false;
    }

    private static bool IsHrDepartment(Employee employee)
    {
        return string.Equals(
                   employee.Department?.Code,
                   "01",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   employee.Department?.Name,
                   "HR",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string GetApprovalErrorMessage(
        SqlException exception)
    {
        var message = exception.Message;

        if (message.Contains(
                "not pending",
                StringComparison.OrdinalIgnoreCase))
        {
            return "This leave request is no longer pending.";
        }

        if (message.Contains(
                "not enough available leave",
                StringComparison.OrdinalIgnoreCase))
        {
            return "The employee does not have enough available leave.";
        }

        if (message.Contains(
                "balance could not be found",
                StringComparison.OrdinalIgnoreCase))
        {
            return "The leave balance could not be found.";
        }

        if (message.Contains(
                "approver account could not be found",
                StringComparison.OrdinalIgnoreCase))
        {
            return "The approver account could not be found.";
        }

        return "The leave request could not be approved.";
    }

    private async Task<UserAccount?> GetCurrentUserAccountAsync()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim == null ||
            !int.TryParse(claim.Value, out var userAccountId))
        {
            return null;
        }

        return await _context.UserAccounts
            .Include(u => u.Employee)
            .AsNoTracking()
            .SingleOrDefaultAsync(u =>
                u.UserAccountId == userAccountId &&
                u.IsActive);
    }

    private async System.Threading.Tasks.Task PopulateLeaveTypeListAsync(
        int employeeId,
        int? selectedLeaveTypeId = null)
    {
        await _leaveBalanceService.EnsureAndAccrueAsync();
        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .Where(lt => lt.IsActive)
            .OrderBy(lt => lt.Name)
            .ToListAsync();

        var balances = await _context.LeaveBalances
            .AsNoTracking()
            .Where(lb => lb.EmployeeId == employeeId)
            .ToDictionaryAsync(
                lb => lb.LeaveTypeId,
                lb => lb.AvailableLeaves);

        ViewBag.LeaveTypes = leaveTypes
            .Select(lt =>
            {
                var balanceText =
                    balances.TryGetValue(
                        lt.LeaveTypeId,
                        out var available)
                        ? $" (Available: {available:0.##} days)"
                        : " (Balance not available)";

                return new SelectListItem
                {
                    Value = lt.LeaveTypeId.ToString(),
                    Text = lt.Name + balanceText,
                    Selected =
                        selectedLeaveTypeId == lt.LeaveTypeId
                };
            })
            .ToList();
    }

    private async System.Threading.Tasks.Task PopulateEmployeeFilterAsync(
        int? selectedEmployeeId = null)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();

        ViewBag.EmployeeFilter = employees
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text =
                    $"{e.EmployeeCode} - {e.FirstName} {e.LastName}",
                Selected =
                    selectedEmployeeId == e.EmployeeId
            })
            .ToList();
    }

    private async System.Threading.Tasks.Task PopulateLeaveTypeFilterAsync(
        int? selectedLeaveTypeId = null)
    {
        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(lt => lt.Name)
            .ToListAsync();

        ViewBag.LeaveTypeFilter = leaveTypes
            .Select(lt => new SelectListItem
            {
                Value = lt.LeaveTypeId.ToString(),
                Text = lt.Name,
                Selected =
                    selectedLeaveTypeId == lt.LeaveTypeId
            })
            .ToList();
    }
}
