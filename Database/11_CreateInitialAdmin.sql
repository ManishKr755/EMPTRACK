
USE [EMPTRACK];
GO

/*
    EMPTRACK - Create Initial Admin Account

    Username: admin
    Password: Admin@123

    Run this script after creating the EMPTRACK database
    and its tables.
*/

DECLARE @Username NVARCHAR(100) = N'admin';

-- ASP.NET Core PasswordHasher-compatible hash for Admin@123
DECLARE @PasswordHash NVARCHAR(255) =
    N'AQAAAAIAAYagAAAAEAARIjNEVWZ3iJmqu8zd7v+NcjTh5PUmgImbdc2CDEHzdxuYgu+KZxc386GddOmMyA==';

IF EXISTS (
    SELECT 1
    FROM dbo.UserAccount
    WHERE Username = @Username
)
BEGIN
    -- Reset the existing Admin password to the documented password.
    UPDATE dbo.UserAccount
    SET PasswordHash = @PasswordHash,
        EmployeeId = NULL,
        EncryptedPassword = NULL,
        IsActive = 1
    WHERE Username = @Username;
END
ELSE
BEGIN
    -- Create the Admin account in a fresh database.
    INSERT INTO dbo.UserAccount
        (EmployeeId, Username, PasswordHash, EncryptedPassword, IsActive)
    VALUES
        (NULL, @Username, @PasswordHash, NULL, 1);
END;

PRINT 'Initial Admin account is ready.';
PRINT 'Username: admin';
PRINT 'Password: Admin@123';
GO
