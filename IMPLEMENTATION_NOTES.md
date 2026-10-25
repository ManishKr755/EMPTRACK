# EMPTRACK implementation and verification notes

Date: 2026-10-25

This is a college-project-focused implementation pass that keeps the existing ASP.NET Core MVC, EF Core, SQL Server, controllers, models, and views. It is not a redesign.

## Implemented source changes

- **Authentication:** cookie expiration is 30 minutes with sliding renewal for a 30-minute idle timeout. The Remember Me input does not extend the lifetime. Each authenticated request validates account activity and linked employee status and refreshes the role/employee claims from the current database record. Unlinked non-Admin accounts are rejected. Unsafe MVC requests receive global antiforgery validation.
- **Employees and accounts:** employee creation creates the login account through the existing onboarding path and wraps employee/account/initial-balance work in a transaction. Generated employee code/serial values are not overwritten by profile edits. Admin may manage employee records; HR and Employee self-service forms expose and save only email/phone; Head Manager creation is limited to Employee role in the caller's department; HR Head creation is limited to Employee/HR in the permitted department scope. Employee list/detail queries are role scoped. Admin can view inactive employees. The separate account reset-password endpoints return Forbidden; normal self-service password change remains available. Stored passwords are not displayed.
- **Departments and teams:** department access/edit actions apply department checks. Teams are readable by authenticated roles, with Manager/supervisor list/detail scope applied. Admin can edit/delete but cannot create teams; HR Head/Head Manager team management is department-scoped. Deletion is blocked when employee references exist and also fails safely if another FK reference exists.
- **Leave:** request lists and detail endpoints scope by role and record; Admin cannot create a personal leave request; Managers cannot approve/reject; approver checks reject self-approval and restrict Head Manager/HR Head to their assigned authority. Request creation checks date order, active leave type, reason, and overlapping pending/approved requests. Approval uses the existing stored procedure that locks pending requests and balances and deducts once; rejection does not deduct. Manual balance Create/Edit endpoints return Forbidden. Balance accrual is period-tracked and uses a serializable transaction to reduce duplicate initialization/accrual; onboarding participates in its existing transaction.
- **Attendance:** Admin can view all but has no self check-in/out identity. Other role queries and detail endpoints are scoped; check-in is self-only, requires an active employee, prevents duplicate daily check-in, and check-out requires an existing same-day check-in. The existing Create/Edit views have no corresponding write actions.
- **Salary and holidays:** salary reads are Admin-wide or own-history-only for other roles; create/edit are Admin-only; delete endpoints are disabled. Holiday create/edit/delete are Admin-only. Duplicate dates are checked in controller code and `15_HolidayDateUniqueConstraint.sql` adds a reviewed unique index after a duplicate-date preflight.
- **Tasks and reviews:** Admin task access is view-only; Manager/Head Manager task create/content edit is scoped to team/department, and assignees can update only task status. Review creation validates reviewer eligibility and rejects self-review; list/detail access is scoped to own, reviewed-by-self, team, or department relationships; editing/deletion is restricted to the reviewer or Admin.
- **Concerns and messages:** concern list/detail/status operations are scoped. Concern creators can edit their own subject while not Closed and can delete their own concern where historical messages do not exist; Admin can view/change status/delete, subject to preserving message history. HR/HR Head status changes are scoped to their permitted department. New replies are disabled; the ConcernMessage model/table and existing history are retained.
- **Announcements:** Admin-only create/edit/delete; list and detail access is server-filtered for `AllExceptAdmin` or a specific department. `14_AnnouncementAudience.sql` adds the required fields while defaulting existing announcements to `AllExceptAdmin`.
- **Notifications:** each user can read and mark read only their own records. Individual delivery validates recipient scope; group delivery inserts one notification row per recipient so every recipient has independent `IsRead`. Admin can send to all active employees, Head Manager/HR Head to their authorized department scope, and Manager to their team. HR/Employee cannot create notifications.
- **Dashboard/directory:** management dashboard counts and recent employee lists are restricted to Admin-wide, Manager-team, Head Manager-department, or HR/HR Head department scope. Employee view shows personal counts only. Employee and attendance filters were scoped so dropdowns do not reveal unrelated employee lists.
- **SQL safety:** `Database/SQL_SCRIPT_REVIEW.md` documents the numbered SQL scripts, idempotency/data effects, risks, and suggested manual review order. The initial Admin SQL no longer embeds a reusable default password/hash; it requires a locally generated ASP.NET Core password hash. `12_DropAuditLog.sql` remains in the archive for traceability but is destructive and is not part of normal setup.

## Manual database work required

No SQL was executed and no database was contacted. Before running the updated code against a database, inspect its current schema and apply only the required scripts manually:

1. `Database/13_AutomaticLeaveBalanceAccrual.sql` if `LeaveBalance.LastAccruedPeriod` is not already present.
2. `Database/14_AnnouncementAudience.sql` before deploying announcement audience code. It adds `AudienceType`, nullable `DepartmentId`, a department FK, and an audience check constraint. Existing announcements receive `AllExceptAdmin`.
3. `Database/15_HolidayDateUniqueConstraint.sql` if the unique holiday-date index is not already present. It stops if duplicate dates exist; resolve those records manually without deleting required history.
4. Review the existing `03_AddForeignKeysAndConstraints.sql` for required unique constraints, especially attendance `(EmployeeId, Date)` and leave balance `(EmployeeId, LeaveTypeId)`, before relying on database concurrency protection.

Back up the database and confirm actual object/constraint names before applying any script. Do not run `12_DropAuditLog.sql` without a separate, explicit decision and backup. `11_CreateInitialAdmin.sql` resets the existing `admin` password if that username exists; replace its placeholder with a locally generated ASP.NET Core PasswordHasher hash and review the target database before use.

## Business rule deliberately not invented

The authority to cancel already approved leave, whether the balance should be restored, and how cancellation history should be recorded remain unresolved. No new approved-leave cancellation workflow or balance-reversal policy was added.

The existing approval procedure counts calendar days, including weekends/holidays. The supplied requirements asked to inspect holiday effects but did not define whether leave should count calendar days or working days; this implementation preserves the existing behavior and documents it rather than inventing a new allocation policy.

## Compiler fix applied in this revision

`Controllers/AnnouncementsController.cs` now explicitly aliases `Task` to `System.Threading.Tasks.Task`. The project also defines `EMPTRACK.Models.Task`, so the two non-generic async helper signatures (`ValidateAudience` and `LoadDepartments`) could otherwise bind ambiguously or be treated as returning the model entity. This resolves the four reported compiler diagnostics at source level. The .NET SDK was not available here, so the fix could not be confirmed by a full compilation.

## Verification status

- ZIP integrity and content checks: performed after packaging.
- C# delimiter scan: passed for all source `.cs` files.
- Project file XML parsing: passed.
- Focused source checks for cookie timeout, disabled balance writes, reset-password denial, audience migration, notification group delivery, and disabled concern replies: passed.
- `dotnet restore`, `dotnet build`, automated tests, SQL Server tests, and authenticated HTTP negative tests: **not run** because the .NET SDK/MSBuild and a test database were unavailable in the environment. Static checks are not a substitute for compilation or runtime tests.
- No SQL script was executed; no live database was contacted.

See `SECURITY_TEST_MATRIX.md` for the role matrix and test cases with execution status.

## Sensitive material

The existing Data Protection key file is retained because legacy encrypted values may depend on it. Treat it as sensitive and do not publish the ZIP broadly. The archive excludes `.git`, `.vs`, build outputs, and machine-specific `.csproj.user` files.
