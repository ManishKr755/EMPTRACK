using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using EMPTRACK.Services;
namespace EMPTRACK.Controllers;

[Authorize]
public class PerformanceReviewsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public PerformanceReviewsController(ApplicationDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    // GET: /PerformanceReviews
    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Index()
    {
        var currentEmployeeId = GetCurrentEmployeeId();

        // Admin can see all reviews.
        if (User.IsInRole("Admin"))
        {
            var allReviews = await _context.PerformanceReviews
                .Include(r => r.Employee)
                .Include(r => r.Reviewer)
                .AsNoTracking()
                .OrderByDescending(r => r.ReviewDate)
                .ThenByDescending(r => r.ReviewId)
                .ToListAsync();

            return View(allReviews);
        }

        // Employee accounts must have an EmployeeId.
        if (!currentEmployeeId.HasValue)
        {
            return Forbid();
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e =>
                e.EmployeeId == currentEmployeeId.Value &&
                e.EmploymentStatus == "Active");

        if (employee == null)
        {
            return Forbid();
        }

        var query = _context.PerformanceReviews
            .Include(r => r.Employee)
            .Include(r => r.Reviewer)
            .AsNoTracking()
            .AsQueryable();

        /*
         * Normal employees can see only their own reviews.
         *
         * Managers and Head Managers can also see reviews
         * that they created.
         */
        if (User.IsInRole("Manager") ||
            User.IsInRole("Head Manager") ||
            User.IsInRole("HR Head") ||
            User.IsInRole("HR"))
        {
            query = query.Where(r =>
                r.EmployeeId == currentEmployeeId.Value ||
                r.ReviewerEmployeeId == currentEmployeeId.Value);
        }
        else
        {
            query = query.Where(r =>
                r.EmployeeId == currentEmployeeId.Value);
        }

        var reviews = await query
            .OrderByDescending(r => r.ReviewDate)
            .ThenByDescending(r => r.ReviewId)
            .ToListAsync();

        return View(reviews);
    }

    // GET: /PerformanceReviews/Details/5
    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var review = await _context.PerformanceReviews
            .Include(r => r.Employee)
            .Include(r => r.Reviewer)
            .AsNoTracking()
            .SingleOrDefaultAsync(r =>
                r.ReviewId == id.Value);

        if (review == null)
        {
            return NotFound();
        }

        if (!await CanViewReviewAsync(review))
        {
            return Forbid();
        }

        return View(review);
    }

    // GET: /PerformanceReviews/Create
    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Create()
    {
        var currentEmployee = await GetCurrentEmployeeAsync();

        if (currentEmployee == null)
        {
            return Forbid();
        }

        if (!CanCreateReviews(currentEmployee))
        {
            return Forbid();
        }

        await PopulateEligibleEmployeeListAsync(currentEmployee);

        var model = new PerformanceReviewCreateViewModel
        {
            ReviewDate = DateOnly.FromDateTime(DateTime.Today),
            Rating = 3
        };

        return View(model);
    }

    // POST: /PerformanceReviews/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Create(
        PerformanceReviewCreateViewModel model)
    {
        var currentEmployee = await GetCurrentEmployeeAsync();

        if (currentEmployee == null)
        {
            return Forbid();
        }

        if (!CanCreateReviews(currentEmployee))
        {
            return Forbid();
        }

        await ValidateCreateReviewInputAsync(
            currentEmployee,
            model.EmployeeId,
            model.ReviewDate,
            model.Rating,
            model.Feedback);

        if (!ModelState.IsValid)
        {
            await PopulateEligibleEmployeeListAsync(
                currentEmployee,
                model.EmployeeId);

            return View(model);
        }

        var review = new PerformanceReview
        {
            EmployeeId = model.EmployeeId,

            // IMPORTANT:
            // ReviewerEmployeeId comes from the authenticated employee.
            ReviewerEmployeeId = currentEmployee.EmployeeId,

            ReviewDate = model.ReviewDate,
            Rating = model.Rating,
            Feedback = model.Feedback.Trim()
        };

        _context.PerformanceReviews.Add(review);

        try
        {
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "CREATE", "PerformanceReview", $"Performance review {review.ReviewId} was created.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The performance review could not be saved.");

            await PopulateEligibleEmployeeListAsync(
                currentEmployee,
                model.EmployeeId);

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Performance review created successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = review.ReviewId });
    }

    // GET: /PerformanceReviews/Edit/5
    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var review = await _context.PerformanceReviews
            .AsNoTracking()
            .SingleOrDefaultAsync(r =>
                r.ReviewId == id.Value);

        if (review == null)
        {
            return NotFound();
        }

        if (!await CanManageReviewAsync(review))
        {
            return Forbid();
        }

        var currentEmployee = await GetCurrentEmployeeAsync();

        if (currentEmployee == null)
        {
            return Forbid();
        }

        await PopulateEligibleEmployeeListAsync(
            currentEmployee,
            review.EmployeeId);

        var model = new PerformanceReviewEditViewModel
        {
            ReviewId = review.ReviewId,
            EmployeeId = review.EmployeeId,
            ReviewDate = review.ReviewDate,
            Rating = review.Rating,
            Feedback = review.Feedback
        };

        return View(model);
    }

    // POST: /PerformanceReviews/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Edit(
        int id,
        PerformanceReviewEditViewModel model)
    {
        if (id != model.ReviewId)
        {
            return NotFound();
        }

        var review = await _context.PerformanceReviews
            .SingleOrDefaultAsync(r =>
                r.ReviewId == id);

        if (review == null)
        {
            return NotFound();
        }

        if (!await CanManageReviewAsync(review))
        {
            return Forbid();
        }

        var currentEmployee = await GetCurrentEmployeeAsync();

        if (currentEmployee == null)
        {
            return Forbid();
        }

        await ValidateEditReviewInputAsync(
            currentEmployee,
            review,
            model.EmployeeId,
            model.ReviewDate,
            model.Rating,
            model.Feedback);

        if (!ModelState.IsValid)
        {
            await PopulateEligibleEmployeeListAsync(
                currentEmployee,
                model.EmployeeId);

            return View(model);
        }

        /*
         * Do NOT take ReviewerEmployeeId from the browser.
         *
         * The existing reviewer remains unchanged.
         */
        review.EmployeeId = model.EmployeeId;
        review.ReviewDate = model.ReviewDate;
        review.Rating = model.Rating;
        review.Feedback = model.Feedback.Trim();

        try
        {
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "UPDATE", "PerformanceReview", $"Performance review {review.ReviewId} was updated.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The performance review could not be updated.");

            await PopulateEligibleEmployeeListAsync(
                currentEmployee,
                model.EmployeeId);

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Performance review updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = review.ReviewId });
    }

    // GET: /PerformanceReviews/Delete/5
    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var review = await _context.PerformanceReviews
            .Include(r => r.Employee)
            .Include(r => r.Reviewer)
            .AsNoTracking()
            .SingleOrDefaultAsync(r =>
                r.ReviewId == id.Value);

        if (review == null)
        {
            return NotFound();
        }

        if (!await CanManageReviewAsync(review))
        {
            return Forbid();
        }

        return View(review);
    }

    // POST: /PerformanceReviews/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var review = await _context.PerformanceReviews
            .SingleOrDefaultAsync(r =>
                r.ReviewId == id);

        if (review == null)
        {
            return NotFound();
        }

        if (!await CanManageReviewAsync(review))
        {
            return Forbid();
        }

        _context.PerformanceReviews.Remove(review);

        try
        {
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "DELETE", "PerformanceReview", $"Performance review {review.ReviewId} was deleted.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The performance review could not be deleted.");

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] =
            "Performance review deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // Authorization helpers
    // ============================================================

    private int? GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");

        if (claim == null)
        {
            return null;
        }

        if (int.TryParse(claim.Value, out var employeeId))
        {
            return employeeId;
        }

        return null;
    }

    private async System.Threading.Tasks.Task<Employee?> GetCurrentEmployeeAsync()
    {
        var employeeId = GetCurrentEmployeeId();

        if (!employeeId.HasValue)
        {
            return null;
        }

        return await _context.Employees
            .SingleOrDefaultAsync(e =>
                e.EmployeeId == employeeId.Value &&
                e.EmploymentStatus == "Active");
    }

    private bool CanCreateReviews(Employee reviewer)
    {
        return User.IsInRole("Manager") ||
               User.IsInRole("Head Manager") ||
               User.IsInRole("HR Head") ||
               User.IsInRole("HR");
    }

    private async System.Threading.Tasks.Task<bool> CanViewReviewAsync(
        PerformanceReview review)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        if (!currentEmployeeId.HasValue)
        {
            return false;
        }

        /*
         * An employee can see their own review.
         */
        if (review.EmployeeId == currentEmployeeId.Value)
        {
            return true;
        }

        /*
         * Managers and HR-type roles can see reviews
         * they created.
         */
        if ((User.IsInRole("Manager") ||
             User.IsInRole("Head Manager") ||
             User.IsInRole("HR Head") ||
             User.IsInRole("HR")) &&
            review.ReviewerEmployeeId == currentEmployeeId.Value)
        {
            return true;
        }

        return false;
    }

    private async System.Threading.Tasks.Task<bool> CanManageReviewAsync(
        PerformanceReview review)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        if (!currentEmployeeId.HasValue)
        {
            return false;
        }

        /*
         * Only the employee who created the review
         * may edit/delete it.
         */
        return review.ReviewerEmployeeId == currentEmployeeId.Value;
    }

    // ============================================================
    // Create validation
    // ============================================================

    private async System.Threading.Tasks.Task ValidateCreateReviewInputAsync(
        Employee reviewer,
        int employeeId,
        DateOnly reviewDate,
        int rating,
        string feedback)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e =>
                e.EmployeeId == employeeId &&
                e.EmploymentStatus == "Active");

        if (employee == null)
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewCreateViewModel.EmployeeId),
                "The selected employee does not exist or is not active.");

            return;
        }

        if (reviewer.EmployeeId == employee.EmployeeId)
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewCreateViewModel.EmployeeId),
                "You cannot review yourself.");
        }

        if (!CanReviewEmployee(reviewer, employee))
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewCreateViewModel.EmployeeId),
                "You are not authorized to review this employee.");
        }

        if (reviewDate > DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewCreateViewModel.ReviewDate),
                "Review date cannot be in the future.");
        }

        if (rating < 1 || rating > 5)
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewCreateViewModel.Rating),
                "Rating must be between 1 and 5.");
        }

        ValidateFeedback(
            feedback,
            nameof(PerformanceReviewCreateViewModel.Feedback));
    }

    // ============================================================
    // Edit validation
    // ============================================================

    private async System.Threading.Tasks.Task ValidateEditReviewInputAsync(
        Employee reviewer,
        PerformanceReview review,
        int employeeId,
        DateOnly reviewDate,
        int rating,
        string feedback)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e =>
                e.EmployeeId == employeeId &&
                e.EmploymentStatus == "Active");

        if (employee == null)
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewEditViewModel.EmployeeId),
                "The selected employee does not exist or is not active.");

            return;
        }

        if (reviewer.EmployeeId == employee.EmployeeId)
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewEditViewModel.EmployeeId),
                "You cannot review yourself.");
        }

        if (!CanReviewEmployee(reviewer, employee))
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewEditViewModel.EmployeeId),
                "You are not authorized to review this employee.");
        }

        if (reviewDate > DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewEditViewModel.ReviewDate),
                "Review date cannot be in the future.");
        }

        if (rating < 1 || rating > 5)
        {
            ModelState.AddModelError(
                nameof(PerformanceReviewEditViewModel.Rating),
                "Rating must be between 1 and 5.");
        }

        ValidateFeedback(
            feedback,
            nameof(PerformanceReviewEditViewModel.Feedback));
    }

    private void ValidateFeedback(
        string? feedback,
        string propertyName)
    {
        if (string.IsNullOrWhiteSpace(feedback))
        {
            ModelState.AddModelError(
                propertyName,
                "Feedback is required.");

            return;
        }

        var trimmedFeedback = feedback.Trim();

        if (trimmedFeedback.Length < 10)
        {
            ModelState.AddModelError(
                propertyName,
                "Feedback must contain at least 10 characters.");
        }

        if (trimmedFeedback.Length > 1000)
        {
            ModelState.AddModelError(
                propertyName,
                "Feedback cannot exceed 1000 characters.");
        }
    }

    // ============================================================
    // Review authorization rule
    // ============================================================

    private bool CanReviewEmployee(
        Employee reviewer,
        Employee employee)
    {
        /*
         * Manager:
         * Can review employees in the same team.
         */
        if (string.Equals(
                reviewer.Role,
                "Manager",
                StringComparison.OrdinalIgnoreCase))
        {
            return reviewer.TeamId.HasValue &&
                   employee.TeamId.HasValue &&
                   reviewer.TeamId.Value == employee.TeamId.Value;
        }

        /*
         * Head Manager:
         * Can review employees in the same department.
         */
        if (string.Equals(
                reviewer.Role,
                "Head Manager",
                StringComparison.OrdinalIgnoreCase))
        {
            return reviewer.DepartmentId.HasValue &&
                   employee.DepartmentId.HasValue &&
                   reviewer.DepartmentId.Value == employee.DepartmentId.Value;
        }

        /*
         * HR Head:
         * Can review employees in the same department
         * where the department relationship exists.
         */
        if (string.Equals(
                reviewer.Role,
                "HR Head",
                StringComparison.OrdinalIgnoreCase))
        {
            return reviewer.DepartmentId.HasValue &&
                   employee.DepartmentId.HasValue &&
                   reviewer.DepartmentId.Value == employee.DepartmentId.Value;
        }

        /*
         * HR is allowed to review when applicable.
         *
         * This still uses the normal ReviewerEmployeeId
         * relationship. No special HR review table exists.
         */
        if (string.Equals(
                reviewer.Role,
                "HR",
                StringComparison.OrdinalIgnoreCase))
        {
            return reviewer.DepartmentId.HasValue &&
                   employee.DepartmentId.HasValue &&
                   reviewer.DepartmentId.Value == employee.DepartmentId.Value;
        }

        return false;
    }

    // ============================================================
    // Dropdown
    // ============================================================

    private async System.Threading.Tasks.Task PopulateEligibleEmployeeListAsync(
        Employee reviewer,
        int? selectedEmployeeId = null)
    {
        IQueryable<Employee> query = _context.Employees
            .AsNoTracking()
            .Where(e =>
                e.EmploymentStatus == "Active" &&
                e.EmployeeId != reviewer.EmployeeId);

        if (string.Equals(
                reviewer.Role,
                "Manager",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!reviewer.TeamId.HasValue)
            {
                ViewBag.Employees = new List<SelectListItem>();
                return;
            }

            query = query.Where(e =>
                e.TeamId == reviewer.TeamId.Value);
        }
        else if (string.Equals(
                     reviewer.Role,
                     "Head Manager",
                     StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                     reviewer.Role,
                     "HR Head",
                     StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                     reviewer.Role,
                     "HR",
                     StringComparison.OrdinalIgnoreCase))
        {
            if (!reviewer.DepartmentId.HasValue)
            {
                ViewBag.Employees = new List<SelectListItem>();
                return;
            }

            query = query.Where(e =>
                e.DepartmentId == reviewer.DepartmentId.Value);
        }
        else
        {
            ViewBag.Employees = new List<SelectListItem>();
            return;
        }

        var employees = await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();

        ViewBag.Employees = employees
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