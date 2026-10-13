using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace EMPTRACK.Controllers;

[Authorize]
public class ConcernMessagesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ConcernMessagesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: /ConcernMessages/AddMessage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMessage(
        ConcernMessageCreateViewModel model)
    {
        var concern = await _context.Concerns
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.ConcernId == model.ConcernId);

        if (concern == null)
        {
            return NotFound();
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        if (!currentEmployeeId.HasValue)
        {
            // Admin is intentionally not an Employee in the existing design,
            // so Admin cannot be stored as ConcernMessage.SenderEmployeeId.
            return Forbid();
        }

        var isConcernOwner =
            concern.EmployeeId == currentEmployeeId.Value;

        var isHrUser =
            User.IsInRole("HR") ||
            User.IsInRole("HR Head");

        if (!isConcernOwner && !isHrUser)
        {
            // Normal employees can only send to their own concerns.
            // Managers do not receive automatic access to all concerns.
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Message is required.";
            return RedirectToConcernDetails(model.ConcernId);
        }

        var messageText = model.Message.Trim();

        if (string.IsNullOrWhiteSpace(messageText))
        {
            TempData["ErrorMessage"] = "Message is required.";
            return RedirectToConcernDetails(model.ConcernId);
        }

        var employeeExists = await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.EmployeeId == currentEmployeeId.Value);

        if (!employeeExists)
        {
            return Forbid();
        }

        var message = new ConcernMessage
        {
            ConcernId = concern.ConcernId,
            SenderEmployeeId = currentEmployeeId.Value,
            Message = messageText,
            CreatedAt = DateTime.Now
        };

        _context.ConcernMessages.Add(message);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The message could not be saved.";

            return RedirectToConcernDetails(concern.ConcernId);
        }

        TempData["SuccessMessage"] = "Message sent successfully.";

        return RedirectToConcernDetails(concern.ConcernId);
    }

    private RedirectToActionResult RedirectToConcernDetails(int concernId)
    {
        return RedirectToAction(
            "Details",
            "Concerns",
            new { id = concernId });
    }

    private int? GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");

        if (claim == null)
        {
            return null;
        }

        return int.TryParse(
            claim.Value,
            out var employeeId)
            ? employeeId
            : null;
    }
}
