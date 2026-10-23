using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

using EMPTRACK.Services;
namespace EMPTRACK.Controllers;

[Authorize]
public class SalaryPaymentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SalaryPaymentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /SalaryPayments
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? employeeId,
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        var currentUser = await GetCurrentUserAccountAsync();

        if (currentUser == null)
        {
            return Forbid();
        }

        var isSalaryAdministrator = IsSalaryAdministrator();

        var query = _context.SalaryPayments
            .Include(p => p.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (!isSalaryAdministrator)
        {
            /*
             * A normal employee can see only their own salary history.
             * The EmployeeId comes from the authenticated UserAccount,
             * not from a browser-supplied value.
             */
            if (!currentUser.EmployeeId.HasValue ||
                currentUser.Employee == null)
            {
                return Forbid();
            }

            query = query.Where(p =>
                p.EmployeeId == currentUser.EmployeeId.Value);

            ViewData["IsOwnHistory"] = true;
        }
        else
        {
            ViewData["IsOwnHistory"] = false;

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Employee.FirstName.Contains(search) ||
                    p.Employee.LastName.Contains(search) ||
                    p.Employee.EmployeeCode.Contains(search) ||
                    p.Employee.Email.Contains(search));
            }

            if (employeeId.HasValue)
            {
                query = query.Where(p =>
                    p.EmployeeId == employeeId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p =>
                    p.PaymentDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p =>
                    p.PaymentDate <= toDate.Value);
            }

            ViewData["Search"] = search;
            ViewData["EmployeeId"] = employeeId;
            ViewData["FromDate"] = fromDate;
            ViewData["ToDate"] = toDate;

            await PopulateEmployeeListAsync(employeeId);
        }

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.PaymentId)
            .ToListAsync();

        return View(payments);
    }

    // GET: /SalaryPayments/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var payment = await _context.SalaryPayments
            .Include(p => p.Employee)
            .AsNoTracking()
            .SingleOrDefaultAsync(p =>
                p.PaymentId == id.Value);

        if (payment == null)
        {
            return NotFound();
        }

        if (!await CanViewPaymentAsync(payment))
        {
            return Forbid();
        }

        return View(payment);
    }

    // GET: /SalaryPayments/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsSalaryAdministrator())
        {
            return Forbid();
        }

        await PopulateEmployeeListAsync();

        var model = new SalaryPaymentCreateViewModel
        {
            PaymentDate = DateOnly.FromDateTime(DateTime.Today)
        };

        return View(model);
    }

    // POST: /SalaryPayments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SalaryPaymentCreateViewModel model)
    {
        if (!IsSalaryAdministrator())
        {
            return Forbid();
        }

        ValidatePaymentDateAndAmount(
            model.PaymentDate,
            model.Amount,
            nameof(SalaryPaymentCreateViewModel.PaymentDate),
            nameof(SalaryPaymentCreateViewModel.Amount));

        var employeeExists = await EmployeeExistsAsync(model.EmployeeId);

        if (!employeeExists)
        {
            ModelState.AddModelError(
                nameof(SalaryPaymentCreateViewModel.EmployeeId),
                "The selected employee does not exist or is not active.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateEmployeeListAsync(model.EmployeeId);
            return View(model);
        }

        var payment = new SalaryPayment
        {
            EmployeeId = model.EmployeeId,
            PaymentDate = model.PaymentDate,
            Amount = model.Amount
        };

        _context.SalaryPayments.Add(payment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The salary payment could not be saved.");

            await PopulateEmployeeListAsync(model.EmployeeId);
            return View(model);
        }

        TempData["SuccessMessage"] =
            "Salary payment created successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = payment.PaymentId });
    }

    // GET: /SalaryPayments/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!IsSalaryAdministrator())
        {
            return Forbid();
        }

        if (!id.HasValue)
        {
            return NotFound();
        }

        var payment = await _context.SalaryPayments
            .AsNoTracking()
            .SingleOrDefaultAsync(p =>
                p.PaymentId == id.Value);

        if (payment == null)
        {
            return NotFound();
        }

        await PopulateEmployeeListAsync(payment.EmployeeId);

        var model = new SalaryPaymentEditViewModel
        {
            PaymentId = payment.PaymentId,
            EmployeeId = payment.EmployeeId,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount
        };

        return View(model);
    }

    // POST: /SalaryPayments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        SalaryPaymentEditViewModel model)
    {
        if (!IsSalaryAdministrator())
        {
            return Forbid();
        }

        if (id != model.PaymentId)
        {
            return NotFound();
        }

        var payment = await _context.SalaryPayments
            .SingleOrDefaultAsync(p =>
                p.PaymentId == id);

        if (payment == null)
        {
            return NotFound();
        }

        ValidatePaymentDateAndAmount(
            model.PaymentDate,
            model.Amount,
            nameof(SalaryPaymentEditViewModel.PaymentDate),
            nameof(SalaryPaymentEditViewModel.Amount));

        var employeeExists = await EmployeeExistsAsync(model.EmployeeId);

        if (!employeeExists)
        {
            ModelState.AddModelError(
                nameof(SalaryPaymentEditViewModel.EmployeeId),
                "The selected employee does not exist or is not active.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateEmployeeListAsync(model.EmployeeId);
            return View(model);
        }

        payment.EmployeeId = model.EmployeeId;
        payment.PaymentDate = model.PaymentDate;
        payment.Amount = model.Amount;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The salary payment could not be updated.");

            await PopulateEmployeeListAsync(model.EmployeeId);
            return View(model);
        }

        TempData["SuccessMessage"] =
            "Salary payment updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = payment.PaymentId });
    }

    // GET: /SalaryPayments/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!IsSalaryAdministrator())
        {
            return Forbid();
        }

        if (!id.HasValue)
        {
            return NotFound();
        }

        var payment = await _context.SalaryPayments
            .Include(p => p.Employee)
            .AsNoTracking()
            .SingleOrDefaultAsync(p =>
                p.PaymentId == id.Value);

        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    // POST: /SalaryPayments/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!IsSalaryAdministrator())
        {
            return Forbid();
        }

        var payment = await _context.SalaryPayments
            .SingleOrDefaultAsync(p =>
                p.PaymentId == id);

        if (payment == null)
        {
            return NotFound();
        }

        _context.SalaryPayments.Remove(payment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The salary payment could not be deleted.";

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] =
            "Salary payment deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // Authorization helpers
    // ============================================================

    private bool IsSalaryAdministrator()
    {
        return User.IsInRole("Admin") ||
               User.IsInRole("HR Head") ||
               User.IsInRole("HR");
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

    private async Task<bool> CanViewPaymentAsync(
        SalaryPayment payment)
    {
        if (IsSalaryAdministrator())
        {
            return true;
        }

        var currentUser = await GetCurrentUserAccountAsync();

        if (currentUser?.EmployeeId == null)
        {
            return false;
        }

        return payment.EmployeeId ==
               currentUser.EmployeeId.Value;
    }

    // ============================================================
    // Validation
    // ============================================================

    private async Task<bool> EmployeeExistsAsync(int employeeId)
    {
        return await _context.Employees
            .AsNoTracking()
            .AnyAsync(e =>
                e.EmployeeId == employeeId &&
                e.EmploymentStatus == "Active");
    }

    private void ValidatePaymentDateAndAmount(
        DateOnly paymentDate,
        decimal amount,
        string paymentDateField,
        string amountField)
    {
        if (paymentDate == default)
        {
            ModelState.AddModelError(
                paymentDateField,
                "Payment date is required.");
        }
        else if (paymentDate > DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(
                paymentDateField,
                "Payment date cannot be in the future.");
        }

        if (amount <= 0)
        {
            ModelState.AddModelError(
                amountField,
                "Payment amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            ModelState.AddModelError(
                amountField,
                "Payment amount can have a maximum of two decimal places.");
        }

        // DECIMAL(10,2) has a maximum value of 99,999,999.99.
        if (amount > 99999999.99m)
        {
            ModelState.AddModelError(
                amountField,
                "Payment amount is too large.");
        }
    }

    // ============================================================
    // Employee dropdown
    // ============================================================

    private async Task PopulateEmployeeListAsync(
        int? selectedEmployeeId = null)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmploymentStatus == "Active")
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
}
