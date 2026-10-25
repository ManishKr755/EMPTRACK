using EMPTRACK.Data;
using EMPTRACK.Models;
using Microsoft.EntityFrameworkCore;

namespace EMPTRACK.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateNotificationAsync(int employeeId, string title, string message)
    {
        var created = await CreateNotificationsAsync(new[] { employeeId }, title, message);
        return created[0];
    }

    public async Task<List<Notification>> CreateNotificationsAsync(IEnumerable<int> employeeIds, string title, string message)
    {
        var recipients = employeeIds.Where(id => id > 0).Distinct().ToList();
        if (recipients.Count == 0) throw new ArgumentException("At least one valid recipient is required.", nameof(employeeIds));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Notification title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Notification message is required.", nameof(message));

        var existingIds = await _context.Employees.AsNoTracking()
            .Where(e => recipients.Contains(e.EmployeeId) && e.EmploymentStatus == "Active")
            .Select(e => e.EmployeeId).ToListAsync();
        if (existingIds.Count != recipients.Count) throw new ArgumentException("One or more recipients do not exist or are inactive.", nameof(employeeIds));

        var now = DateTime.Now;
        var notifications = recipients.Select(employeeId => new Notification
        {
            EmployeeId = employeeId,
            Title = title.Trim(),
            Message = message.Trim(),
            IsRead = false,
            CreatedAt = now
        }).ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return notifications;
    }
}
