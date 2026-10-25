/*
    EMPTRACK - Fresh database schema

    Use only for a NEW, EMPTY EMPTRACK database. Create the EMPTRACK database
    first, then run this script. Do not run against a database containing data.

    This script creates current tables, relationships, constraints, indexes,
    and the leave approval procedure used by LeaveRequestsController.
    It intentionally contains NO sample/demo/seed records and does not create
    the Admin account. Use 03_CreateInitialAdmin.sql separately for that.
*/
USE EMPTRACK;
GO

/* =========================================================
   1. Employee
   ========================================================= */

CREATE TABLE Employee
(
    EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeCode CHAR(6) NOT NULL UNIQUE,
    EmployeeSerial INT NOT NULL UNIQUE,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    Role NVARCHAR(30) NOT NULL,
    TeamId INT NULL,
    DepartmentId INT NULL,
    JoiningDate DATE NOT NULL,
    EmploymentStatus NVARCHAR(20) NOT NULL
);
GO


/* =========================================================
   2. UserAccount
   ========================================================= */

CREATE TABLE UserAccount
(
    UserAccountId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NULL,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    EncryptedPassword NVARCHAR(2048) NULL,
    IsActive BIT NOT NULL
);
GO


/* =========================================================
   3. Department
   ========================================================= */

CREATE TABLE Department
(
    DepartmentId INT IDENTITY(1,1) PRIMARY KEY,
    Code CHAR(2) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    HeadManagerEmployeeId INT NULL,
    HeadHREmployeeId INT NULL
);
GO


/* =========================================================
   4. Team
   ========================================================= */

CREATE TABLE Team
(
    TeamId INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    ManagerEmployeeId INT NOT NULL UNIQUE
);
GO


/* =========================================================
   5. Attendance
   ========================================================= */

CREATE TABLE Attendance
(
    AttendanceId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    [Date] DATE NOT NULL,
    CheckInTime TIME NOT NULL,
    CheckOutTime TIME NULL
);
GO


/* =========================================================
   6. LeaveType
   ========================================================= */

CREATE TABLE LeaveType
(
    LeaveTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    AllocationType NVARCHAR(10) NOT NULL,
    AllocationLimit DECIMAL(5,2) NOT NULL,
    IsActive BIT NOT NULL
);
GO


/* =========================================================
   7. LeaveBalance
   ========================================================= */

CREATE TABLE LeaveBalance
(
    LeaveBalanceId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    LeaveTypeId INT NOT NULL,
    AvailableLeaves DECIMAL(5,2) NOT NULL,
    LastAccruedPeriod DATE NULL
);
GO


/* =========================================================
   8. LeaveRequest
   ========================================================= */

CREATE TABLE LeaveRequest
(
    LeaveRequestId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    LeaveTypeId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Reason NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL,
    ApprovedByUserId INT NULL,
    ApprovedAt DATETIME2 NULL
);
GO


/* =========================================================
   9. Task
   ========================================================= */

CREATE TABLE [Task]
(
    TaskId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000) NOT NULL,
    AssignedEmployeeId INT NOT NULL,
    CreatedByEmployeeId INT NOT NULL,
    DueDate DATE NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    Priority NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   10. PerformanceReview
   ========================================================= */

CREATE TABLE PerformanceReview
(
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    ReviewerEmployeeId INT NOT NULL,
    ReviewDate DATE NOT NULL,
    Rating INT NOT NULL,
    Feedback NVARCHAR(1000) NOT NULL
);
GO


/* =========================================================
   11. SalaryPayment
   ========================================================= */

CREATE TABLE SalaryPayment
(
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    PaymentDate DATE NOT NULL,
    Amount DECIMAL(10,2) NOT NULL
);
GO


/* =========================================================
   12. Notification
   ========================================================= */

CREATE TABLE Notification
(
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    IsRead BIT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   13. Announcement
   ========================================================= */

CREATE TABLE Announcement
(
    AnnouncementId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(2000) NOT NULL,
    PublishedAt DATETIME2 NOT NULL,
    AudienceType NVARCHAR(30) NOT NULL CONSTRAINT DF_Announcement_AudienceType DEFAULT ('AllExceptAdmin'),
    DepartmentId INT NULL
);
GO


/* =========================================================
   14. Holiday
   ========================================================= */

CREATE TABLE Holiday
(
    HolidayId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    [Date] DATE NOT NULL
);
GO


/* =========================================================
   15. Concern
   ========================================================= */

CREATE TABLE Concern
(
    ConcernId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    Subject NVARCHAR(200) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   16. ConcernMessage
   ========================================================= */

CREATE TABLE ConcernMessage
(
    MessageId INT IDENTITY(1,1) PRIMARY KEY,
    ConcernId INT NOT NULL,
    SenderEmployeeId INT NOT NULL,
    Message NVARCHAR(2000) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO

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

-- Announcement -> Department
IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Announcement_Department_DepartmentId'
      AND parent_object_id = OBJECT_ID('dbo.Announcement')
)
BEGIN
    ALTER TABLE dbo.Announcement WITH CHECK
    ADD CONSTRAINT FK_Announcement_Department_DepartmentId
        FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(DepartmentId);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Announcement_AudienceType'
      AND parent_object_id = OBJECT_ID('dbo.Announcement')
)
BEGIN
    ALTER TABLE dbo.Announcement
    ADD CONSTRAINT CK_Announcement_AudienceType
        CHECK (AudienceType IN ('AllExceptAdmin', 'Department'));
END;
GO

-- Holiday dates must be unique (the fresh table is empty at this point).
CREATE UNIQUE INDEX UX_Holiday_Date ON dbo.Holiday([Date]);
GO

-- Leave approval procedure
GO
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
