using EMPTRACK.Data;
using EMPTRACK.Models;

namespace EMPTRACK.Services;

public interface IAuditLogService
{
    System.Threading.Tasks.Task LogAsync(
        int userAccountId,
        string action,
        string target,
        string? description);
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task LogAsync(
        int userAccountId,
        string action,
        string target,
        string? description)
    {
        var auditLog = new AuditLog
        {
            UserAccountId = userAccountId,
            Action = action,
            Target = target,
            DateTime = DateTime.Now,
            Description = description ?? string.Empty
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}