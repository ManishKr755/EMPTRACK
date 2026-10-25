
using EMPTRACK.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMPTRACK.ViewComponents;

public class TeamNavigationViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public TeamNavigationViewComponent(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = UserClaimsPrincipal;

        // This component adds a link only for HR and Employees.
        if (!user.IsInRole("HR") &&
            !user.IsInRole("Employee"))
        {
            return Content(string.Empty);
        }

        var rawEmployeeId =
            user.FindFirst("EmployeeId")?.Value;

        if (!int.TryParse(rawEmployeeId, out var employeeId) ||
            employeeId <= 0)
        {
            return Content(string.Empty);
        }

        // Read the current team assignment from the database.
        // Do not rely on a potentially stale TeamId cookie claim.
        var teamId = await _context.Employees
            .AsNoTracking()
            .Where(e =>
                e.EmployeeId == employeeId &&
                (e.Role == "HR" || e.Role == "Employee") &&
                e.TeamId.HasValue)
            .Select(e => e.TeamId)
            .SingleOrDefaultAsync();

        if (!teamId.HasValue)
        {
            return Content(string.Empty);
        }

        return View(teamId.Value);
    }
}
