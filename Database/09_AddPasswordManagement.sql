USE EMPTRACK;
GO

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
