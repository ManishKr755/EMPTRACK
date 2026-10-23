/*
    EMPTRACK automatic leave balance accrual support.
    Run this script once against the EMPTRACK database before running
    the updated application. Existing balances are preserved; their
    accrual tracking begins in the current calendar period.
*/
USE EMPTRACK;
GO

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
