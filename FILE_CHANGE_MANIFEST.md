# File change manifest

Compared with the original uploaded EMPTRACK source archive. Machine-specific files and build outputs are excluded from the comparison.

- Changed files: 48
- Added files: 7
- Removed source files: 0

## Changed files
- `Controllers/AccountController.cs`
- `Controllers/AnnouncementsController.cs`
- `Controllers/AttendanceController.cs`
- `Controllers/ConcernMessagesController.cs`
- `Controllers/ConcernsController.cs`
- `Controllers/DashboardController.cs`
- `Controllers/DepartmentsController.cs`
- `Controllers/EmployeesController.cs`
- `Controllers/HolidaysController.cs`
- `Controllers/LeaveBalancesController.cs`
- `Controllers/LeaveRequestsController.cs`
- `Controllers/LeaveTypesController.cs`
- `Controllers/NotificationsController.cs`
- `Controllers/PerformanceReviewsController.cs`
- `Controllers/SalaryPaymentsController.cs`
- `Controllers/TasksController.cs`
- `Controllers/TeamsController.cs`
- `Controllers/UserAccountsController.cs`
- `Data/ApplicationDbContext.cs`
- `Database/11_CreateInitialAdmin.sql`
- `Models/Announcement.cs`
- `Models/ViewModels/AnnouncementCreateViewModel.cs`
- `Models/ViewModels/AnnouncementEditViewModel.cs`
- `Models/ViewModels/EmployeeDetailsViewModel.cs`
- `Models/ViewModels/NotificationCreateViewModel.cs`
- `Program.cs`
- `Services/LeaveBalanceService.cs`
- `Services/NotificationService.cs`
- `Views/Announcements/Create.cshtml`
- `Views/Announcements/Details.cshtml`
- `Views/Announcements/Edit.cshtml`
- `Views/Announcements/Index.cshtml`
- `Views/Concerns/Details.cshtml`
- `Views/Employees/Details.cshtml`
- `Views/Employees/Edit.cshtml`
- `Views/Holidays/Details.cshtml`
- `Views/Holidays/Index.cshtml`
- `Views/Notifications/Create.cshtml`
- `Views/SalaryPayments/Details.cshtml`
- `Views/SalaryPayments/Index.cshtml`
- `Views/Tasks/Index.cshtml`
- `Views/Teams/Details.cshtml`
- `Views/Teams/Index.cshtml`
- `Views/UserAccounts/Create.cshtml`
- `Views/UserAccounts/Details.cshtml`
- `Views/UserAccounts/Edit.cshtml`
- `Views/UserAccounts/Index.cshtml`
- `Views/UserAccounts/ResetPassword.cshtml`

## Added files
- `Database/14_AnnouncementAudience.sql`
- `Database/15_HolidayDateUniqueConstraint.sql`
- `Database/SQL_SCRIPT_REVIEW.md`
- `IMPLEMENTATION_NOTES.md`
- `SECURITY_TEST_MATRIX.md`
- `Views/Concerns/Edit.cshtml`
- `Views/Teams/Delete.cshtml`

## Removed source files
- None.

## Excluded from distributable ZIP
- `.git/`, `.vs/`, `bin/`, `obj/`, `settings.VisualStudio.json`, and `*.csproj.user` files (repository history, IDE/build artifacts, and machine-specific settings).
- The existing Data Protection key file is retained for compatibility with existing protected values; treat it as sensitive.
