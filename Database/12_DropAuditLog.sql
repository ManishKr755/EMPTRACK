USE EMPTRACK;
GO

/*
    EMPTRACK - Remove Audit Log

    Run this ONCE on an existing EMPTRACK database that was created
    before the Audit Log feature was removed.
    New databases do not need this script.
*/

IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.AuditLog;
    PRINT 'AuditLog table dropped.';
END
ELSE
BEGIN
    PRINT 'AuditLog table does not exist. Nothing to do.';
END;
GO
