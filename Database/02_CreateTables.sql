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
    CheckInTime TIME NULL,
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
    AvailableLeaves DECIMAL(5,2) NOT NULL
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
    Description NVARCHAR(1000) NULL,
    AssignedEmployeeId INT NOT NULL,
    CreatedByEmployeeId INT NOT NULL,
    DueDate DATE NULL,
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
    Feedback NVARCHAR(1000) NULL
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
    PublishedAt DATETIME2 NOT NULL
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


/* =========================================================
   17. AuditLog
   ========================================================= */

CREATE TABLE AuditLog
(
    AuditLogId INT IDENTITY(1,1) PRIMARY KEY,
    UserAccountId INT NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    Target NVARCHAR(200) NOT NULL,
    [DateTime] DATETIME2 NOT NULL,
    Description NVARCHAR(1000) NULL
);
GO