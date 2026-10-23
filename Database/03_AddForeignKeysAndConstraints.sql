USE EMPTRACK;
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

