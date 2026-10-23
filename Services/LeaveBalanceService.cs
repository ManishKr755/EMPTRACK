using EMPTRACK.Data;
using EMPTRACK.Models;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
namespace EMPTRACK.Services;

/// <summary>
/// Keeps one balance per active employee and active leave type.
/// Monthly allocations renew on the first day of each calendar month;
/// annual allocations renew on 1 January. Missed periods are caught up
/// the next time this service runs.
/// </summary>
public sealed class LeaveBalanceService
{
    private readonly ApplicationDbContext _context;

    public LeaveBalanceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task EnsureAndAccrueAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeEmployees = await _context.Employees
            .Where(e => e.EmploymentStatus == "Active")
            .Select(e => e.EmployeeId)
            .ToListAsync(cancellationToken);

        var activeTypes = await _context.LeaveTypes
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        if (activeEmployees.Count == 0 || activeTypes.Count == 0)
            return;

        var employeeIds = activeEmployees.ToHashSet();
        var typeIds = activeTypes.Select(t => t.LeaveTypeId).ToHashSet();
        var balances = await _context.LeaveBalances
            .Where(b => employeeIds.Contains(b.EmployeeId) && typeIds.Contains(b.LeaveTypeId))
            .ToListAsync(cancellationToken);

        var balanceLookup = balances.ToDictionary(b => (b.EmployeeId, b.LeaveTypeId));
        var changed = false;

        foreach (var employeeId in activeEmployees)
        {
            foreach (var type in activeTypes)
            {
                var periodStart = GetCurrentPeriodStart(type.AllocationType, today);
                if (!balanceLookup.TryGetValue((employeeId, type.LeaveTypeId), out var balance))
                {
                    balance = new LeaveBalance
                    {
                        EmployeeId = employeeId,
                        LeaveTypeId = type.LeaveTypeId,
                        AvailableLeaves = type.AllocationLimit,
                        LastAccruedPeriod = periodStart
                    };
                    _context.LeaveBalances.Add(balance);
                    balanceLookup[(employeeId, type.LeaveTypeId)] = balance;
                    changed = true;
                    continue;
                }

                // Existing balances predate accrual tracking. Start tracking from
                // the current period without granting duplicate leave immediately.
                if (balance.LastAccruedPeriod == null)
                {
                    balance.LastAccruedPeriod = periodStart;
                    changed = true;
                    continue;
                }

                var nextPeriod = GetNextPeriodStart(type.AllocationType, balance.LastAccruedPeriod.Value);
                while (nextPeriod <= periodStart)
                {
                    balance.AvailableLeaves += type.AllocationLimit;
                    balance.LastAccruedPeriod = nextPeriod;
                    nextPeriod = GetNextPeriodStart(type.AllocationType, nextPeriod);
                    changed = true;
                }
            }
        }

        if (changed)
            await _context.SaveChangesAsync(cancellationToken);
    }

    public static DateOnly GetCurrentPeriodStart(string allocationType, DateOnly today) =>
        string.Equals(allocationType, "Monthly", StringComparison.OrdinalIgnoreCase)
            ? new DateOnly(today.Year, today.Month, 1)
            : new DateOnly(today.Year, 1, 1);

    private static DateOnly GetNextPeriodStart(string allocationType, DateOnly periodStart) =>
        string.Equals(allocationType, "Monthly", StringComparison.OrdinalIgnoreCase)
            ? periodStart.AddMonths(1)
            : periodStart.AddYears(1);
}
