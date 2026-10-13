USE EMPTRACK;
GO

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
