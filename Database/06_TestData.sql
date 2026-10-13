/*
    ============================================================
    EMPTRACK - Test / Seed Data
    File: 06_TestData.sql
    Purpose: Small controlled development/test dataset

    IMPORTANT:
    - Database only. No C# / ASP.NET code.
    - Does not create or alter tables.
    - Does not use IDENTITY_INSERT.
    - Does not assume identity values.
    - Designed to be reasonably safe to run more than once.
    - Does not delete existing data.
    - UserAccount password hashes are DEVELOPMENT-ONLY seed values.
      Authentication has not yet been implemented.
    ============================================================
*/

USE EMPTRACK;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO


/* ============================================================
   1. VARIABLES
   ============================================================ */

DECLARE
    @DepartmentHR INT,
    @DepartmentIT INT,
    @DepartmentFinance INT,

    @HRHead INT,
    @HREmployee INT,
    @ITHead INT,
    @ITManager INT,
    @ITEmployee1 INT,
    @ITEmployee2 INT,
    @FinanceHead INT,
    @FinanceManager INT,
    @FinanceEmployee INT,
    @UnassignedEmployee INT,

    @ITDevelopmentTeam INT,
    @FinanceOperationsTeam INT,

    @AdminUser INT,
    @HRHeadUser INT,
    @ITHeadUser INT,
    @ITManagerUser INT,
    @Employee1User INT,
    @FinanceManagerUser INT,

    @CasualLeave INT,
    @SickLeave INT,
    @AnnualLeave INT,

    @Concern1 INT,
    @Concern2 INT;


/* ============================================================
   2. DEPARTMENTS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Department
    WHERE Code = '01'
)
BEGIN
    INSERT INTO dbo.Department
    (
        Code,
        Name,
        HeadManagerEmployeeId,
        HeadHREmployeeId
    )
    VALUES
    (
        '01',
        N'HR',
        NULL,
        NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Department
    WHERE Code = '02'
)
BEGIN
    INSERT INTO dbo.Department
    (
        Code,
        Name,
        HeadManagerEmployeeId,
        HeadHREmployeeId
    )
    VALUES
    (
        '02',
        N'IT',
        NULL,
        NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Department
    WHERE Code = '03'
)
BEGIN
    INSERT INTO dbo.Department
    (
        Code,
        Name,
        HeadManagerEmployeeId,
        HeadHREmployeeId
    )
    VALUES
    (
        '03',
        N'Finance',
        NULL,
        NULL
    );
END;


/* Capture generated Department IDs. */

SELECT @DepartmentHR = DepartmentId
FROM dbo.Department
WHERE Code = '01';

SELECT @DepartmentIT = DepartmentId
FROM dbo.Department
WHERE Code = '02';

SELECT @DepartmentFinance = DepartmentId
FROM dbo.Department
WHERE Code = '03';


/* ============================================================
   3. EMPLOYEES
   ============================================================ */

/*
    Employee codes:
    
    HR:
    014101 = HR Employee, serial 101
    012102 = HR Head, serial 102

    IT:
    022103 = IT Head Manager, serial 103
    023104 = IT Manager, serial 104
    024105 = IT Employee, serial 105
    024106 = IT Employee, serial 106

    Finance:
    032107 = Finance Head Manager, serial 107
    033108 = Finance Manager, serial 108
    034109 = Finance Employee, serial 109

    Unassigned:
    000110 = provisional/unassigned employee, serial 110

    The unassigned employee cannot have a real department code
    while DepartmentId is NULL, so 000110 is deliberately
    provisional until the employee is assigned.
*/


/* HR Head */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 102
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '012102',
        102,
        N'Priya',
        N'Sharma',
        N'priya.sharma@employeetest.local',
        N'9000000102',
        N'HR Head',
        NULL,
        @DepartmentHR,
        '2022-04-01',
        N'Active'
    );
END;


/* HR Employee */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 101
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '014101',
        101,
        N'Neha',
        N'Patel',
        N'neha.patel@employeetest.local',
        N'9000000101',
        N'Employee',
        NULL,
        @DepartmentHR,
        '2023-06-15',
        N'Active'
    );
END;


/* IT Head Manager */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 103
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '022103',
        103,
        N'Rahul',
        N'Mehta',
        N'rahul.mehta@employeetest.local',
        N'9000000103',
        N'Head Manager',
        NULL,
        @DepartmentIT,
        '2021-01-10',
        N'Active'
    );
END;


/* IT Manager */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 104
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '023104',
        104,
        N'Amit',
        N'Desai',
        N'amit.desai@employeetest.local',
        N'9000000104',
        N'Manager',
        NULL,
        @DepartmentIT,
        '2022-08-01',
        N'Active'
    );
END;


/* IT Employee 1 */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 105
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '024105',
        105,
        N'Rohan',
        N'Shah',
        N'rohan.shah@employeetest.local',
        N'9000000105',
        N'Employee',
        NULL,
        @DepartmentIT,
        '2024-02-12',
        N'Active'
    );
END;


/* IT Employee 2 */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 106
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '024106',
        106,
        N'Karan',
        N'Joshi',
        N'karan.joshi@employeetest.local',
        N'9000000106',
        N'Employee',
        NULL,
        @DepartmentIT,
        '2024-07-01',
        N'Active'
    );
END;


/* Finance Head Manager */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 107
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '032107',
        107,
        N'Sneha',
        N'Kapoor',
        N'sneha.kapoor@employeetest.local',
        N'9000000107',
        N'Head Manager',
        NULL,
        @DepartmentFinance,
        '2020-11-05',
        N'Active'
    );
END;


/* Finance Manager */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 108
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '033108',
        108,
        N'Vikram',
        N'Rao',
        N'vikram.rao@employeetest.local',
        N'9000000108',
        N'Manager',
        NULL,
        @DepartmentFinance,
        '2022-03-21',
        N'Active'
    );
END;


/* Finance Employee */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 109
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '034109',
        109,
        N'Anjali',
        N'Joshi',
        N'anjali.joshi@employeetest.local',
        N'9000000109',
        N'Employee',
        NULL,
        @DepartmentFinance,
        '2024-05-20',
        N'Active'
    );
END;


/* Newly hired / unassigned employee */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employee
    WHERE EmployeeSerial = 110
)
BEGIN
    INSERT INTO dbo.Employee
    (
        EmployeeCode,
        EmployeeSerial,
        FirstName,
        LastName,
        Email,
        Phone,
        Role,
        TeamId,
        DepartmentId,
        JoiningDate,
        EmploymentStatus
    )
    VALUES
    (
        '000110',
        110,
        N'Arjun',
        N'Kumar',
        N'arjun.kumar@employeetest.local',
        N'9000000110',
        N'Employee',
        NULL,
        NULL,
        '2026-08-01',
        N'Active'
    );
END;


/* ============================================================
   4. CAPTURE EMPLOYEE IDs
   ============================================================ */

SELECT @HRHead = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 102;

SELECT @HREmployee = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 101;

SELECT @ITHead = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 103;

SELECT @ITManager = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 104;

SELECT @ITEmployee1 = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 105;

SELECT @ITEmployee2 = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 106;

SELECT @FinanceHead = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 107;

SELECT @FinanceManager = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 108;

SELECT @FinanceEmployee = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 109;

SELECT @UnassignedEmployee = EmployeeId
FROM dbo.Employee
WHERE EmployeeSerial = 110;


/* ============================================================
   5. UPDATE DEPARTMENT HEADS
   ============================================================ */

/*
    HR:
    HeadHREmployeeId = HR Head

    IT and Finance:
    HeadManagerEmployeeId = respective Head Manager
*/

UPDATE dbo.Department
SET HeadHREmployeeId = @HRHead
WHERE DepartmentId = @DepartmentHR;

UPDATE dbo.Department
SET HeadManagerEmployeeId = @ITHead
WHERE DepartmentId = @DepartmentIT;

UPDATE dbo.Department
SET HeadManagerEmployeeId = @FinanceHead
WHERE DepartmentId = @DepartmentFinance;


/* ============================================================
   6. TEAMS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Team
    WHERE Name = N'IT Development'
      AND DepartmentId = @DepartmentIT
)
BEGIN
    INSERT INTO dbo.Team
    (
        DepartmentId,
        Name,
        ManagerEmployeeId
    )
    VALUES
    (
        @DepartmentIT,
        N'IT Development',
        @ITManager
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Team
    WHERE Name = N'Finance Operations'
      AND DepartmentId = @DepartmentFinance
)
BEGIN
    INSERT INTO dbo.Team
    (
        DepartmentId,
        Name,
        ManagerEmployeeId
    )
    VALUES
    (
        @DepartmentFinance,
        N'Finance Operations',
        @FinanceManager
    );
END;


/* Capture Team IDs. */

SELECT @ITDevelopmentTeam = TeamId
FROM dbo.Team
WHERE Name = N'IT Development'
  AND DepartmentId = @DepartmentIT;

SELECT @FinanceOperationsTeam = TeamId
FROM dbo.Team
WHERE Name = N'Finance Operations'
  AND DepartmentId = @DepartmentFinance;


/* ============================================================
   7. ASSIGN EMPLOYEES TO TEAMS
   ============================================================ */

UPDATE dbo.Employee
SET TeamId = @ITDevelopmentTeam
WHERE EmployeeId IN
(
    @ITManager,
    @ITEmployee1,
    @ITEmployee2
);

UPDATE dbo.Employee
SET TeamId = @FinanceOperationsTeam
WHERE EmployeeId IN
(
    @FinanceManager,
    @FinanceEmployee
);


/* ============================================================
   8. USER ACCOUNTS
   ============================================================ */

/*
    DEVELOPMENT-ONLY PASSWORD HASH

    This is an ASP.NET Core Identity V3-compatible PBKDF2 hash
    generated for development seed data.

    It must NOT be treated as the final authentication design.

    Development password represented by the hash:
    DevOnly!123

    The eventual authentication implementation must replace or
    properly manage these credentials.
*/

DECLARE @DevelopmentPasswordHash NVARCHAR(255) =
N'AQEAAACghgEAEAAAADuynZXZPiSugiRjvRA/rCsgAAAA6SrXw8IhH08KElfT1gqunEYvqGu72cQOyooxZPh2Zyg=';


/* Admin */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.UserAccount
    WHERE Username = N'admin'
)
BEGIN
    INSERT INTO dbo.UserAccount
    (
        EmployeeId,
        Username,
        PasswordHash,
        IsActive
    )
    VALUES
    (
        NULL,
        N'admin',
        @DevelopmentPasswordHash,
        1
    );
END;


/* HR Head */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.UserAccount
    WHERE Username = N'hrhead'
)
BEGIN
    INSERT INTO dbo.UserAccount
    (
        EmployeeId,
        Username,
        PasswordHash,
        IsActive
    )
    VALUES
    (
        @HRHead,
        N'hrhead',
        @DevelopmentPasswordHash,
        1
    );
END;


/* IT Head */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.UserAccount
    WHERE Username = N'ithead'
)
BEGIN
    INSERT INTO dbo.UserAccount
    (
        EmployeeId,
        Username,
        PasswordHash,
        IsActive
    )
    VALUES
    (
        @ITHead,
        N'ithead',
        @DevelopmentPasswordHash,
        1
    );
END;


/* IT Manager */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.UserAccount
    WHERE Username = N'itmanager'
)
BEGIN
    INSERT INTO dbo.UserAccount
    (
        EmployeeId,
        Username,
        PasswordHash,
        IsActive
    )
    VALUES
    (
        @ITManager,
        N'itmanager',
        @DevelopmentPasswordHash,
        1
    );
END;


/* IT Employee */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.UserAccount
    WHERE Username = N'employee1'
)
BEGIN
    INSERT INTO dbo.UserAccount
    (
        EmployeeId,
        Username,
        PasswordHash,
        IsActive
    )
    VALUES
    (
        @ITEmployee1,
        N'employee1',
        @DevelopmentPasswordHash,
        1
    );
END;


/* Capture UserAccount IDs. */

SELECT @AdminUser = UserAccountId
FROM dbo.UserAccount
WHERE Username = N'admin';

SELECT @HRHeadUser = UserAccountId
FROM dbo.UserAccount
WHERE Username = N'hrhead';

SELECT @ITHeadUser = UserAccountId
FROM dbo.UserAccount
WHERE Username = N'ithead';

SELECT @ITManagerUser = UserAccountId
FROM dbo.UserAccount
WHERE Username = N'itmanager';

SELECT @Employee1User = UserAccountId
FROM dbo.UserAccount
WHERE Username = N'employee1';


/* ============================================================
   9. LEAVE TYPES
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveType
    WHERE Name = N'Casual Leave'
)
BEGIN
    INSERT INTO dbo.LeaveType
    (
        Name,
        IsActive
    )
    VALUES
    (
        N'Casual Leave',
        1
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveType
    WHERE Name = N'Sick Leave'
)
BEGIN
    INSERT INTO dbo.LeaveType
    (
        Name,
        IsActive
    )
    VALUES
    (
        N'Sick Leave',
        1
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveType
    WHERE Name = N'Annual Leave'
)
BEGIN
    INSERT INTO dbo.LeaveType
    (
        Name,
        IsActive
    )
    VALUES
    (
        N'Annual Leave',
        1
    );
END;


/* Capture LeaveType IDs. */

SELECT @CasualLeave = LeaveTypeId
FROM dbo.LeaveType
WHERE Name = N'Casual Leave';

SELECT @SickLeave = LeaveTypeId
FROM dbo.LeaveType
WHERE Name = N'Sick Leave';

SELECT @AnnualLeave = LeaveTypeId
FROM dbo.LeaveType
WHERE Name = N'Annual Leave';


/* ============================================================
   10. LEAVE BALANCES
   ============================================================ */

/* IT Employee 1 */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveBalance
    WHERE EmployeeId = @ITEmployee1
      AND LeaveTypeId = @CasualLeave
)
BEGIN
    INSERT INTO dbo.LeaveBalance
    (
        EmployeeId,
        LeaveTypeId,
        AvailableLeaves
    )
    VALUES
    (
        @ITEmployee1,
        @CasualLeave,
        10
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveBalance
    WHERE EmployeeId = @ITEmployee1
      AND LeaveTypeId = @SickLeave
)
BEGIN
    INSERT INTO dbo.LeaveBalance
    (
        EmployeeId,
        LeaveTypeId,
        AvailableLeaves
    )
    VALUES
    (
        @ITEmployee1,
        @SickLeave,
        8
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveBalance
    WHERE EmployeeId = @ITEmployee1
      AND LeaveTypeId = @AnnualLeave
)
BEGIN
    INSERT INTO dbo.LeaveBalance
    (
        EmployeeId,
        LeaveTypeId,
        AvailableLeaves
    )
    VALUES
    (
        @ITEmployee1,
        @AnnualLeave,
        15
    );
END;


/* Finance Employee */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveBalance
    WHERE EmployeeId = @FinanceEmployee
      AND LeaveTypeId = @CasualLeave
)
BEGIN
    INSERT INTO dbo.LeaveBalance
    (
        EmployeeId,
        LeaveTypeId,
        AvailableLeaves
    )
    VALUES
    (
        @FinanceEmployee,
        @CasualLeave,
        10
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveBalance
    WHERE EmployeeId = @FinanceEmployee
      AND LeaveTypeId = @SickLeave
)
BEGIN
    INSERT INTO dbo.LeaveBalance
    (
        EmployeeId,
        LeaveTypeId,
        AvailableLeaves
    )
    VALUES
    (
        @FinanceEmployee,
        @SickLeave,
        8
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveBalance
    WHERE EmployeeId = @FinanceEmployee
      AND LeaveTypeId = @AnnualLeave
)
BEGIN
    INSERT INTO dbo.LeaveBalance
    (
        EmployeeId,
        LeaveTypeId,
        AvailableLeaves
    )
    VALUES
    (
        @FinanceEmployee,
        @AnnualLeave,
        15
    );
END;


/* ============================================================
   11. LEAVE REQUESTS
   ============================================================ */

/* Pending request */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveRequest
    WHERE EmployeeId = @ITEmployee1
      AND StartDate = '2026-09-07'
      AND EndDate = '2026-09-08'
)
BEGIN
    INSERT INTO dbo.LeaveRequest
    (
        EmployeeId,
        LeaveTypeId,
        StartDate,
        EndDate,
        Reason,
        Status,
        ApprovedByUserId,
        ApprovedAt
    )
    VALUES
    (
        @ITEmployee1,
        @CasualLeave,
        '2026-09-07',
        '2026-09-08',
        N'Personal work',
        N'Pending',
        NULL,
        NULL
    );
END;


/* Approved request */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveRequest
    WHERE EmployeeId = @FinanceEmployee
      AND StartDate = '2026-08-17'
      AND EndDate = '2026-08-18'
)
BEGIN
    INSERT INTO dbo.LeaveRequest
    (
        EmployeeId,
        LeaveTypeId,
        StartDate,
        EndDate,
        Reason,
        Status,
        ApprovedByUserId,
        ApprovedAt
    )
    VALUES
    (
        @FinanceEmployee,
        @AnnualLeave,
        '2026-08-17',
        '2026-08-18',
        N'Family trip',
        N'Approved',
        @FinanceManagerUser,
        '2026-08-10T10:30:00'
    );
END;


/* Rejected request */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.LeaveRequest
    WHERE EmployeeId = @ITEmployee1
      AND StartDate = '2026-07-20'
      AND EndDate = '2026-07-22'
)
BEGIN
    INSERT INTO dbo.LeaveRequest
    (
        EmployeeId,
        LeaveTypeId,
        StartDate,
        EndDate,
        Reason,
        Status,
        ApprovedByUserId,
        ApprovedAt
    )
    VALUES
    (
        @ITEmployee1,
        @CasualLeave,
        '2026-07-20',
        '2026-07-22',
        N'Extended personal leave',
        N'Rejected',
        @ITManagerUser,
        '2026-07-15T14:00:00'
    );
END;


/* ============================================================
   12. TASKS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Task
    WHERE Title = N'Prepare monthly report'
)
BEGIN
    INSERT INTO dbo.Task
    (
        Title,
        Description,
        AssignedEmployeeId,
        CreatedByEmployeeId,
        DueDate,
        Status,
        Priority,
        CreatedAt
    )
    VALUES
    (
        N'Prepare monthly report',
        N'Prepare the monthly departmental report.',
        @FinanceEmployee,
        @FinanceManager,
        '2026-09-05',
        N'Pending',
        N'Medium',
        '2026-08-25T09:00:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Task
    WHERE Title = N'Review employee records'
)
BEGIN
    INSERT INTO dbo.Task
    (
        Title,
        Description,
        AssignedEmployeeId,
        CreatedByEmployeeId,
        DueDate,
        Status,
        Priority,
        CreatedAt
    )
    VALUES
    (
        N'Review employee records',
        N'Review current employee information for accuracy.',
        @HREmployee,
        @HRHead,
        '2026-09-03',
        N'In Progress',
        N'High',
        '2026-08-24T10:00:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Task
    WHERE Title = N'Update department documentation'
)
BEGIN
    INSERT INTO dbo.Task
    (
        Title,
        Description,
        AssignedEmployeeId,
        CreatedByEmployeeId,
        DueDate,
        Status,
        Priority,
        CreatedAt
    )
    VALUES
    (
        N'Update department documentation',
        N'Update technical department documentation.',
        @ITEmployee1,
        @ITManager,
        '2026-08-28',
        N'Completed',
        N'Low',
        '2026-08-20T11:30:00'
    );
END;


/* ============================================================
   13. PERFORMANCE REVIEWS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.PerformanceReview
    WHERE EmployeeId = @ITEmployee1
      AND ReviewDate = '2026-06-30'
)
BEGIN
    INSERT INTO dbo.PerformanceReview
    (
        EmployeeId,
        ReviewerEmployeeId,
        ReviewDate,
        Rating,
        Feedback
    )
    VALUES
    (
        @ITEmployee1,
        @ITManager,
        '2026-06-30',
        4,
        N'Consistently delivers assigned development work and communicates well with the team.'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.PerformanceReview
    WHERE EmployeeId = @FinanceEmployee
      AND ReviewDate = '2026-06-30'
)
BEGIN
    INSERT INTO dbo.PerformanceReview
    (
        EmployeeId,
        ReviewerEmployeeId,
        ReviewDate,
        Rating,
        Feedback
    )
    VALUES
    (
        @FinanceEmployee,
        @FinanceManager,
        '2026-06-30',
        4,
        N'Accurate work with good attention to monthly reporting deadlines.'
    );
END;


/* ============================================================
   14. SALARY PAYMENTS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.SalaryPayment
    WHERE EmployeeId = @ITEmployee1
      AND PaymentDate = '2026-08-01'
)
BEGIN
    INSERT INTO dbo.SalaryPayment
    (
        EmployeeId,
        PaymentDate,
        Amount
    )
    VALUES
    (
        @ITEmployee1,
        '2026-08-01',
        50000.00
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.SalaryPayment
    WHERE EmployeeId = @ITManager
      AND PaymentDate = '2026-08-01'
)
BEGIN
    INSERT INTO dbo.SalaryPayment
    (
        EmployeeId,
        PaymentDate,
        Amount
    )
    VALUES
    (
        @ITManager,
        '2026-08-01',
        65000.00
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.SalaryPayment
    WHERE EmployeeId = @FinanceEmployee
      AND PaymentDate = '2026-08-01'
)
BEGIN
    INSERT INTO dbo.SalaryPayment
    (
        EmployeeId,
        PaymentDate,
        Amount
    )
    VALUES
    (
        @FinanceEmployee,
        '2026-08-01',
        35000.00
    );
END;


/* ============================================================
   15. NOTIFICATIONS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Notification
    WHERE EmployeeId = @ITEmployee1
      AND Title = N'Leave request update'
)
BEGIN
    INSERT INTO dbo.Notification
    (
        EmployeeId,
        Title,
        Message,
        IsRead,
        CreatedAt
    )
    VALUES
    (
        @ITEmployee1,
        N'Leave request update',
        N'Your leave request is currently pending review.',
        0,
        '2026-08-27T09:00:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Notification
    WHERE EmployeeId = @FinanceEmployee
      AND Title = N'Company announcement'
)
BEGIN
    INSERT INTO dbo.Notification
    (
        EmployeeId,
        Title,
        Message,
        IsRead,
        CreatedAt
    )
    VALUES
    (
        @FinanceEmployee,
        N'Company announcement',
        N'A new company announcement has been published.',
        1,
        '2026-08-25T09:30:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Notification
    WHERE EmployeeId = @ITEmployee1
      AND Title = N'Task assigned'
)
BEGIN
    INSERT INTO dbo.Notification
    (
        EmployeeId,
        Title,
        Message,
        IsRead,
        CreatedAt
    )
    VALUES
    (
        @ITEmployee1,
        N'Task assigned',
        N'A new task has been assigned to you.',
        0,
        '2026-08-20T12:00:00'
    );
END;


/* ============================================================
   16. ANNOUNCEMENTS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Announcement
    WHERE Title = N'Monthly Meeting'
)
BEGIN
    INSERT INTO dbo.Announcement
    (
        Title,
        Content,
        PublishedAt
    )
    VALUES
    (
        N'Monthly Meeting',
        N'The monthly company meeting will be held on the first working day of next month.',
        '2026-08-25T10:00:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Announcement
    WHERE Title = N'Company Holiday Notice'
)
BEGIN
    INSERT INTO dbo.Announcement
    (
        Title,
        Content,
        PublishedAt
    )
    VALUES
    (
        N'Company Holiday Notice',
        N'Please note the upcoming company holiday listed in the holiday calendar.',
        '2026-08-26T10:00:00'
    );
END;


/* ============================================================
   17. HOLIDAYS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Holiday
    WHERE Date = '2026-08-15'
)
BEGIN
    INSERT INTO dbo.Holiday
    (
        Name,
        Date
    )
    VALUES
    (
        N'Independence Day',
        '2026-08-15'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Holiday
    WHERE Date = '2026-09-01'
)
BEGIN
    INSERT INTO dbo.Holiday
    (
        Name,
        Date
    )
    VALUES
    (
        N'Company Foundation Day',
        '2026-09-01'
    );
END;


/* ============================================================
   18. CONCERNS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Concern
    WHERE EmployeeId = @ITEmployee1
      AND Subject = N'Equipment issue'
)
BEGIN
    INSERT INTO dbo.Concern
    (
        EmployeeId,
        Subject,
        Status,
        CreatedAt
    )
    VALUES
    (
        @ITEmployee1,
        N'Equipment issue',
        N'In Progress',
        '2026-08-20T09:00:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Concern
    WHERE EmployeeId = @FinanceEmployee
      AND Subject = N'Leave clarification'
)
BEGIN
    INSERT INTO dbo.Concern
    (
        EmployeeId,
        Subject,
        Status,
        CreatedAt
    )
    VALUES
    (
        @FinanceEmployee,
        N'Leave clarification',
        N'Resolved',
        '2026-08-18T11:00:00'
    );
END;


/* Capture Concern IDs. */

SELECT @Concern1 = ConcernId
FROM dbo.Concern
WHERE EmployeeId = @ITEmployee1
  AND Subject = N'Equipment issue';

SELECT @Concern2 = ConcernId
FROM dbo.Concern
WHERE EmployeeId = @FinanceEmployee
  AND Subject = N'Leave clarification';


/* ============================================================
   19. CONCERN MESSAGES
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.ConcernMessage
    WHERE ConcernId = @Concern1
      AND SenderEmployeeId = @ITEmployee1
      AND Message = N'My development laptop is having intermittent charging issues.'
)
BEGIN
    INSERT INTO dbo.ConcernMessage
    (
        ConcernId,
        SenderEmployeeId,
        Message,
        CreatedAt
    )
    VALUES
    (
        @Concern1,
        @ITEmployee1,
        N'My development laptop is having intermittent charging issues.',
        '2026-08-20T09:05:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.ConcernMessage
    WHERE ConcernId = @Concern1
      AND SenderEmployeeId = @ITManager
      AND Message = N'The equipment issue has been forwarded for technical inspection.'
)
BEGIN
    INSERT INTO dbo.ConcernMessage
    (
        ConcernId,
        SenderEmployeeId,
        Message,
        CreatedAt
    )
    VALUES
    (
        @Concern1,
        @ITManager,
        N'The equipment issue has been forwarded for technical inspection.',
        '2026-08-20T11:00:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.ConcernMessage
    WHERE ConcernId = @Concern2
      AND SenderEmployeeId = @FinanceEmployee
      AND Message = N'Please clarify whether annual leave includes the company holiday.'
)
BEGIN
    INSERT INTO dbo.ConcernMessage
    (
        ConcernId,
        SenderEmployeeId,
        Message,
        CreatedAt
    )
    VALUES
    (
        @Concern2,
        @FinanceEmployee,
        N'Please clarify whether annual leave includes the company holiday.',
        '2026-08-18T11:05:00'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.ConcernMessage
    WHERE ConcernId = @Concern2
      AND SenderEmployeeId = @FinanceManager
      AND Message = N'The company holiday is not deducted from your annual leave balance.'
)
BEGIN
    INSERT INTO dbo.ConcernMessage
    (
        ConcernId,
        SenderEmployeeId,
        Message,
        CreatedAt
    )
    VALUES
    (
        @Concern2,
        @FinanceManager,
        N'The company holiday is not deducted from your annual leave balance.',
        '2026-08-18T13:00:00'
    );
END;


/* ============================================================
   20. AUDIT LOG
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AuditLog
    WHERE UserAccountId = @AdminUser
      AND Action = N'Login'
      AND Target = N'UserAccount'
)
BEGIN
    INSERT INTO dbo.AuditLog
    (
        UserAccountId,
        Action,
        Target,
        DateTime,
        Description
    )
    VALUES
    (
        @AdminUser,
        N'Login',
        N'UserAccount',
        '2026-08-28T08:30:00',
        N'Development administrator login test record.'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AuditLog
    WHERE UserAccountId = @AdminUser
      AND Action = N'Create Employee'
      AND Target = N'Employee'
)
BEGIN
    INSERT INTO dbo.AuditLog
    (
        UserAccountId,
        Action,
        Target,
        DateTime,
        Description
    )
    VALUES
    (
        @AdminUser,
        N'Create Employee',
        N'Employee',
        '2026-08-28T08:35:00',
        N'Development seed employee creation test record.'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AuditLog
    WHERE UserAccountId = @HRHeadUser
      AND Action = N'Update Employee'
      AND Target = N'Employee'
)
BEGIN
    INSERT INTO dbo.AuditLog
    (
        UserAccountId,
        Action,
        Target,
        DateTime,
        Description
    )
    VALUES
    (
        @HRHeadUser,
        N'Update Employee',
        N'Employee',
        '2026-08-28T08:40:00',
        N'Development employee update test record.'
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM dbo.AuditLog
    WHERE UserAccountId = @ITManagerUser
      AND Action = N'Approve Leave'
      AND Target = N'LeaveRequest'
)
BEGIN
    INSERT INTO dbo.AuditLog
    (
        UserAccountId,
        Action,
        Target,
        DateTime,
        Description
    )
    VALUES
    (
        @ITManagerUser,
        N'Approve Leave',
        N'LeaveRequest',
        '2026-08-10T10:35:00',
        N'Development leave approval test record.'
    );
END;


/* ============================================================
   21. FINAL MESSAGE
   ============================================================ */

PRINT 'EMPTRACK test/seed data script completed.';
PRINT 'No existing records were intentionally deleted.';
PRINT 'Identity values were generated by SQL Server.';
PRINT 'Admin is stored only in UserAccount and has EmployeeId = NULL.';
PRINT 'The unassigned employee has DepartmentId = NULL and TeamId = NULL.';
GO