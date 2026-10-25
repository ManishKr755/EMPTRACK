/*
    EMPTRACK - Update an existing database to the current application schema

    Use this only against an existing EMPTRACK database whose base tables are
    already present. Back up the database first and review the target database.
    This script does not create tables, seed demo data, drop tables, or delete rows.

    It adds missing feature columns/constraints, initializes the leave accrual
    period marker for existing balances that lack one, checks duplicate holiday
    dates before adding the unique index, and updates the approval procedure.

    If legacy rows conflict with the current C# model's required fields, this
    script prints a warning and leaves that column unchanged for manual review.
*/
USE EMPTRACK;
GO
-- Leave allocation columns and rules (for older schemas)
/*
    Prompt 14 update:
    Each LeaveType now has one allocation mode and one limit.

    Existing rows are initialized as Annually with 1 leave so the new
    NOT NULL columns can be added safely. Admin can edit them afterward.
*/

IF COL_LENGTH('dbo.LeaveType', 'AllocationType') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveType
    ADD AllocationType NVARCHAR(10) NOT NULL
        CONSTRAINT DF_LeaveType_AllocationType DEFAULT ('Annually');
END;
GO

IF COL_LENGTH('dbo.LeaveType', 'AllocationLimit') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveType
    ADD AllocationLimit DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_LeaveType_AllocationLimit DEFAULT (1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_LeaveType_AllocationType'
)
BEGIN
    ALTER TABLE dbo.LeaveType
    ADD CONSTRAINT CK_LeaveType_AllocationType
        CHECK (AllocationType IN ('Annually', 'Monthly'));
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_LeaveType_AllocationLimit_Positive'
)
BEGIN
    ALTER TABLE dbo.LeaveType
    ADD CONSTRAINT CK_LeaveType_AllocationLimit_Positive
        CHECK (AllocationLimit > 0);
END;
GO

GO

-- Password-management schema and account uniqueness
/* Adds encrypted password storage and one-account-per-employee protection. */
IF COL_LENGTH('dbo.UserAccount', 'EncryptedPassword') IS NULL
BEGIN
    ALTER TABLE dbo.UserAccount
    ADD EncryptedPassword NVARCHAR(2048) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_UserAccount_EmployeeId'
      AND object_id = OBJECT_ID('dbo.UserAccount')
)
BEGIN
    IF EXISTS (
        SELECT EmployeeId
        FROM dbo.UserAccount
        WHERE EmployeeId IS NOT NULL
        GROUP BY EmployeeId
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51000, 'Cannot create one-account-per-employee index because duplicate employee accounts exist. Delete the old duplicate accounts first.', 1;
    END;

    CREATE UNIQUE INDEX UX_UserAccount_EmployeeId
        ON dbo.UserAccount(EmployeeId)
        WHERE EmployeeId IS NOT NULL;
END;
GO

GO

-- Automatic leave-balance accrual tracking
/*
    EMPTRACK automatic leave balance accrual support.
    Run this script once against the EMPTRACK database before running
    the updated application. Existing balances are preserved; their
    accrual tracking begins in the current calendar period.
*/
IF COL_LENGTH('dbo.LeaveBalance', 'LastAccruedPeriod') IS NULL
BEGIN
    ALTER TABLE dbo.LeaveBalance
        ADD LastAccruedPeriod DATE NULL;
END;
GO

-- Existing balances start tracking from the current period so that
-- deploying this feature does not unexpectedly grant backdated leave.
UPDATE lb
SET LastAccruedPeriod = CASE
    WHEN lt.AllocationType = 'Monthly'
        THEN DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
    ELSE DATEFROMPARTS(YEAR(GETDATE()), 1, 1)
END
FROM dbo.LeaveBalance lb
INNER JOIN dbo.LeaveType lt ON lt.LeaveTypeId = lb.LeaveTypeId
WHERE lb.LastAccruedPeriod IS NULL;
GO

GO

-- Announcement audience targeting
/*
  EMPTRACK manual migration: announcement audience targeting.
  Prerequisite: back up the database and verify the current dbo.Announcement and dbo.Department names.
  Existing rows are preserved and receive AudienceType='AllExceptAdmin', which keeps them visible to non-Admin users.
  Apply once during a maintenance window before deploying code that reads these columns. Do not run automatically.
*/
IF COL_LENGTH('dbo.Announcement', 'AudienceType') IS NULL
BEGIN
    ALTER TABLE dbo.Announcement ADD AudienceType nvarchar(30) NOT NULL
        CONSTRAINT DF_Announcement_AudienceType DEFAULT ('AllExceptAdmin') WITH VALUES;
END;
GO
IF COL_LENGTH('dbo.Announcement', 'DepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.Announcement ADD DepartmentId int NULL;
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Announcement_Department_DepartmentId')
BEGIN
    ALTER TABLE dbo.Announcement WITH CHECK ADD CONSTRAINT FK_Announcement_Department_DepartmentId
        FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(DepartmentId);
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Announcement_AudienceType')
BEGIN
    ALTER TABLE dbo.Announcement ADD CONSTRAINT CK_Announcement_AudienceType
        CHECK (AudienceType IN ('AllExceptAdmin', 'Department'));
END;
GO

GO

-- Foreign keys, unique constraints, and check constraints
-- =============================================
-- 1. FOREIGN KEYS
-- =============================================

-- ---------------------------------------------
-- UserAccount -> Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_UserAccount_Employee'
)
BEGIN
    ALTER TABLE dbo.UserAccount
    ADD CONSTRAINT FK_UserAccount_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO



-- ---------------------------------------------
-- UserAccount -> Employee (one account per employee)
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_UserAccount_EmployeeId'
      AND object_id = OBJECT_ID('dbo.UserAccount')
)
BEGIN
    CREATE UNIQUE INDEX UX_UserAccount_EmployeeId
        ON dbo.UserAccount(EmployeeId)
        WHERE EmployeeId IS NOT NULL;
END;
GO

-- ---------------------------------------------
-- Employee -> Team
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Employee_Team'
)
BEGIN
    ALTER TABLE dbo.Employee
    ADD CONSTRAINT FK_Employee_Team
        FOREIGN KEY (TeamId)
        REFERENCES dbo.Team(TeamId);
END;
GO


-- ---------------------------------------------
-- Employee -> Department
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Employee_Department'
)
BEGIN
    ALTER TABLE dbo.Employee
    ADD CONSTRAINT FK_Employee_Department
        FOREIGN KEY (DepartmentId)
        REFERENCES dbo.Department(DepartmentId);
END;
GO


-- ---------------------------------------------
-- Team -> Department
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Team_Department'
)
BEGIN
    ALTER TABLE dbo.Team
    ADD CONSTRAINT FK_Team_Department
        FOREIGN KEY (DepartmentId)
        REFERENCES dbo.Department(DepartmentId);
END;
GO


-- ---------------------------------------------
-- Team -> Manager Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Team_ManagerEmployee'
)
BEGIN
    ALTER TABLE dbo.Team
    ADD CONSTRAINT FK_Team_ManagerEmployee
        FOREIGN KEY (ManagerEmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- Department -> Head Manager
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Department_HeadManagerEmployee'
)
BEGIN
    ALTER TABLE dbo.Department
    ADD CONSTRAINT FK_Department_HeadManagerEmployee
        FOREIGN KEY (HeadManagerEmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- Department -> HR Head
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Department_HeadHREmployee'
)
BEGIN
    ALTER TABLE dbo.Department
    ADD CONSTRAINT FK_Department_HeadHREmployee
        FOREIGN KEY (HeadHREmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- Attendance -> Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Attendance_Employee'
)
BEGIN
    ALTER TABLE dbo.Attendance
    ADD CONSTRAINT FK_Attendance_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- LeaveBalance -> Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_LeaveBalance_Employee'
)
BEGIN
    ALTER TABLE dbo.LeaveBalance
    ADD CONSTRAINT FK_LeaveBalance_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- LeaveBalance -> LeaveType
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_LeaveBalance_LeaveType'
)
BEGIN
    ALTER TABLE dbo.LeaveBalance
    ADD CONSTRAINT FK_LeaveBalance_LeaveType
        FOREIGN KEY (LeaveTypeId)
        REFERENCES dbo.LeaveType(LeaveTypeId);
END;
GO


-- ---------------------------------------------
-- LeaveRequest -> Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_LeaveRequest_Employee'
)
BEGIN
    ALTER TABLE dbo.LeaveRequest
    ADD CONSTRAINT FK_LeaveRequest_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- LeaveRequest -> LeaveType
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_LeaveRequest_LeaveType'
)
BEGIN
    ALTER TABLE dbo.LeaveRequest
    ADD CONSTRAINT FK_LeaveRequest_LeaveType
        FOREIGN KEY (LeaveTypeId)
        REFERENCES dbo.LeaveType(LeaveTypeId);
END;
GO


-- ---------------------------------------------
-- LeaveRequest -> UserAccount
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_LeaveRequest_ApprovedByUser'
)
BEGIN
    ALTER TABLE dbo.LeaveRequest
    ADD CONSTRAINT FK_LeaveRequest_ApprovedByUser
        FOREIGN KEY (ApprovedByUserId)
        REFERENCES dbo.UserAccount(UserAccountId);
END;
GO


-- ---------------------------------------------
-- Task -> Assigned Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Task_AssignedEmployee'
)
BEGIN
    ALTER TABLE dbo.Task
    ADD CONSTRAINT FK_Task_AssignedEmployee
        FOREIGN KEY (AssignedEmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- Task -> Created By Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Task_CreatedByEmployee'
)
BEGIN
    ALTER TABLE dbo.Task
    ADD CONSTRAINT FK_Task_CreatedByEmployee
        FOREIGN KEY (CreatedByEmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- PerformanceReview -> Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_PerformanceReview_Employee'
)
BEGIN
    ALTER TABLE dbo.PerformanceReview
    ADD CONSTRAINT FK_PerformanceReview_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- PerformanceReview -> Reviewer Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_PerformanceReview_ReviewerEmployee'
)
BEGIN
    ALTER TABLE dbo.PerformanceReview
    ADD CONSTRAINT FK_PerformanceReview_ReviewerEmployee
        FOREIGN KEY (ReviewerEmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- SalaryPayment -> Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_SalaryPayment_Employee'
)
BEGIN
    ALTER TABLE dbo.SalaryPayment
    ADD CONSTRAINT FK_SalaryPayment_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- Notification -> Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Notification_Employee'
)
BEGIN
    ALTER TABLE dbo.Notification
    ADD CONSTRAINT FK_Notification_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- Concern -> Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Concern_Employee'
)
BEGIN
    ALTER TABLE dbo.Concern
    ADD CONSTRAINT FK_Concern_Employee
        FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- ---------------------------------------------
-- ConcernMessage -> Concern
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ConcernMessage_Concern'
)
BEGIN
    ALTER TABLE dbo.ConcernMessage
    ADD CONSTRAINT FK_ConcernMessage_Concern
        FOREIGN KEY (ConcernId)
        REFERENCES dbo.Concern(ConcernId);
END;
GO


-- ---------------------------------------------
-- ConcernMessage -> Sender Employee
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ConcernMessage_SenderEmployee'
)
BEGIN
    ALTER TABLE dbo.ConcernMessage
    ADD CONSTRAINT FK_ConcernMessage_SenderEmployee
        FOREIGN KEY (SenderEmployeeId)
        REFERENCES dbo.Employee(EmployeeId);
END;
GO


-- =============================================
-- 3. UNIQUE CONSTRAINTS
-- =============================================

-- ---------------------------------------------
-- Attendance: one record per employee per date
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_Attendance_Employee_Date'
)
BEGIN
    ALTER TABLE dbo.Attendance
    ADD CONSTRAINT UQ_Attendance_Employee_Date
        UNIQUE (EmployeeId, Date);
END;
GO


-- ---------------------------------------------
-- LeaveBalance: one balance per employee
-- per leave type
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_LeaveBalance_Employee_LeaveType'
)
BEGIN
    ALTER TABLE dbo.LeaveBalance
    ADD CONSTRAINT UQ_LeaveBalance_Employee_LeaveType
        UNIQUE (EmployeeId, LeaveTypeId);
END;
GO

-- =============================================
-- 4. CHECK CONSTRAINTS
-- =============================================

-- ---------------------------------------------
-- PerformanceReview: rating must be between 1 and 5
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_PerformanceReview_Rating'
)
BEGIN
    ALTER TABLE dbo.PerformanceReview
    ADD CONSTRAINT CK_PerformanceReview_Rating
        CHECK (Rating BETWEEN 1 AND 5);
END;
GO


-- ---------------------------------------------
-- LeaveBalance: available leaves cannot be negative
-- ---------------------------------------------
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_LeaveBalance_AvailableLeaves_NonNegative'
)
BEGIN
    ALTER TABLE dbo.LeaveBalance
    ADD CONSTRAINT CK_LeaveBalance_AvailableLeaves_NonNegative
        CHECK (AvailableLeaves >= 0);
END;
GO


-- =============================================
-- LEAVE ALLOCATION RULES
-- =============================================

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_LeaveType_AllocationType'
)
BEGIN
    ALTER TABLE dbo.LeaveType
    ADD CONSTRAINT CK_LeaveType_AllocationType
        CHECK (AllocationType IN ('Annually', 'Monthly'));
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_LeaveType_AllocationLimit_Positive'
)
BEGIN
    ALTER TABLE dbo.LeaveType
    ADD CONSTRAINT CK_LeaveType_AllocationLimit_Positive
        CHECK (AllocationLimit > 0);
END;
GO

GO

-- Unique holiday dates; stops if duplicates exist
/* Manual, non-destructive migration for unique holiday dates.
   Review and back up the database first. This script does not delete or merge rows.
   If duplicates already exist, it stops and requires manual review before rerunning.
   Run after the existing table/constraint scripts and before deploying the updated holiday controller.
*/
IF EXISTS (SELECT [Date] FROM dbo.Holiday GROUP BY [Date] HAVING COUNT(*) > 1)
BEGIN
    THROW 51000, 'Duplicate holiday dates exist. Resolve them manually without deleting required history before applying this constraint.', 1;
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Holiday_Date' AND object_id = OBJECT_ID('dbo.Holiday'))
BEGIN
    CREATE UNIQUE INDEX UX_Holiday_Date ON dbo.Holiday([Date]);
END;
GO

GO

-- Reconcile four legacy column definitions with current required C# properties.
-- Do not invent values for missing data: skip the alteration and print a warning.
IF COL_LENGTH('dbo.Attendance', 'CheckInTime') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Attendance WHERE CheckInTime IS NULL)
        PRINT 'WARNING: Attendance.CheckInTime contains NULL values; it remains nullable. Resolve these rows manually before changing the column.';
    ELSE
        ALTER TABLE dbo.Attendance ALTER COLUMN CheckInTime TIME NOT NULL;
END;
GO

IF COL_LENGTH('dbo.Task', 'DueDate') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Task WHERE DueDate IS NULL)
        PRINT 'WARNING: Task.DueDate contains NULL values; it remains nullable. Resolve these rows manually before changing the column.';
    ELSE
        ALTER TABLE dbo.Task ALTER COLUMN DueDate DATE NOT NULL;
END;
GO

IF COL_LENGTH('dbo.Task', 'Description') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Task WHERE Description IS NULL OR DATALENGTH(Description) > 4000)
        PRINT 'WARNING: Task.Description has NULL or over-2000-character values; it remains unchanged. Review these rows manually.';
    ELSE
        ALTER TABLE dbo.Task ALTER COLUMN Description NVARCHAR(2000) NOT NULL;
END;
GO

IF COL_LENGTH('dbo.PerformanceReview', 'Feedback') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.PerformanceReview WHERE Feedback IS NULL OR DATALENGTH(Feedback) > 2000)
        PRINT 'WARNING: PerformanceReview.Feedback has NULL or over-1000-character values; it remains unchanged. Review these rows manually.';
    ELSE
        ALTER TABLE dbo.PerformanceReview ALTER COLUMN Feedback NVARCHAR(1000) NOT NULL;
END;
GO

GO

-- Leave approval stored procedure used by LeaveRequestsController
/*
    ============================================================
    EMPTRACK - Leave Approval Workflow
    Included as the current leave-approval procedure
    Purpose: Safely approve a pending leave request and deduct
             the employee's leave balance in one transaction.

    IMPORTANT:
    - Does not create or alter tables.
    - Uses the existing LeaveRequest and LeaveBalance tables.
    - ApprovedByUserId stores UserAccount.UserAccountId.
    - The application performs approval authorization before
      calling this procedure.
    ============================================================
*/
CREATE OR ALTER PROCEDURE dbo.ApproveLeaveRequest
    @LeaveRequestId INT,
    @ApprovedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @EmployeeId INT,
        @LeaveTypeId INT,
        @StartDate DATE,
        @EndDate DATE,
        @Status NVARCHAR(20),
        @NumberOfDays INT,
        @AvailableLeaves DECIMAL(5,2);

    BEGIN TRY
        BEGIN TRANSACTION;

        /* ---------------------------------------------
           1. Verify the approver account exists/active.
           --------------------------------------------- */
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.UserAccount
            WHERE UserAccountId = @ApprovedByUserId
              AND IsActive = 1
        )
        BEGIN
            THROW 51001, 'The approver account could not be found.', 1;
        END;


        /* ---------------------------------------------
           2. Lock the request while it is being approved.
              This prevents a second approval/rejection
              from changing the same request at the same time.
           --------------------------------------------- */
        SELECT
            @EmployeeId = EmployeeId,
            @LeaveTypeId = LeaveTypeId,
            @StartDate = StartDate,
            @EndDate = EndDate,
            @Status = Status
        FROM dbo.LeaveRequest WITH (UPDLOCK, HOLDLOCK)
        WHERE LeaveRequestId = @LeaveRequestId;


        IF @EmployeeId IS NULL
        BEGIN
            THROW 51002, 'Leave request was not found.', 1;
        END;


        /* ---------------------------------------------
           3. Only Pending requests can be approved.
           --------------------------------------------- */
        IF @Status <> N'Pending'
        BEGIN
            THROW 51003, 'This leave request is not pending.', 1;
        END;


        /* ---------------------------------------------
           4. Calculate the requested number of days.
           --------------------------------------------- */
        SET @NumberOfDays =
            DATEDIFF(DAY, @StartDate, @EndDate) + 1;

        IF @NumberOfDays <= 0
        BEGIN
            THROW 51004, 'The leave request has an invalid date range.', 1;
        END;


        /* ---------------------------------------------
           5. Lock the balance row.

              UPDLOCK + HOLDLOCK is important here.
              If two requests use the same balance row,
              only one transaction can check/deduct it
              at a time.
           --------------------------------------------- */
        SELECT
            @AvailableLeaves = AvailableLeaves
        FROM dbo.LeaveBalance WITH (UPDLOCK, HOLDLOCK)
        WHERE EmployeeId = @EmployeeId
          AND LeaveTypeId = @LeaveTypeId;


        IF @AvailableLeaves IS NULL
        BEGIN
            THROW 51005, 'The leave balance could not be found.', 1;
        END;


        /* ---------------------------------------------
           6. Check the balance before deduction.
           --------------------------------------------- */
        IF @AvailableLeaves < @NumberOfDays
        BEGIN
            THROW 51006, 'The employee does not have enough available leave.', 1;
        END;


        /* ---------------------------------------------
           7. Deduct the requested leave.
           --------------------------------------------- */
        UPDATE dbo.LeaveBalance
        SET AvailableLeaves =
            AvailableLeaves - @NumberOfDays
        WHERE EmployeeId = @EmployeeId
          AND LeaveTypeId = @LeaveTypeId;


        /* ---------------------------------------------
           8. Mark the request as approved.
           --------------------------------------------- */
        UPDATE dbo.LeaveRequest
        SET
            Status = N'Approved',
            ApprovedByUserId = @ApprovedByUserId,
            ApprovedAt = SYSDATETIME()
        WHERE LeaveRequestId = @LeaveRequestId
          AND Status = N'Pending';


        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 51007, 'This leave request is no longer pending.', 1;
        END;


        /* ---------------------------------------------
           9. Everything succeeded.
           --------------------------------------------- */
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;
END;
GO
