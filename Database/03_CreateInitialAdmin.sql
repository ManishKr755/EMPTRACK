USE [EMPTRACK];
GO

/*
    EMPTRACK - Secure initial Admin account provisioning.

    IMPORTANT:
    - This script deliberately contains no default password or reusable hash.
    - Generate an ASP.NET Core PasswordHasher<UserAccount> hash locally for a
      strong, unique password, then replace the placeholder below before running.
    - Never commit the resulting hash or password to source control.
    - Review the target database and back it up first. This script updates an
      existing username 'admin' to keep one known Admin account; review this
      behavior before use if the existing Admin account is already configured.
*/
DECLARE @Username NVARCHAR(100) = N'admin';
DECLARE @PasswordHash NVARCHAR(255) = N'<REPLACE_WITH_LOCALLY_GENERATED_ASPNETCORE_PASSWORD_HASH>';

IF @PasswordHash = N'<REPLACE_WITH_LOCALLY_GENERATED_ASPNETCORE_PASSWORD_HASH>'
    THROW 51010, 'Set @PasswordHash to a locally generated ASP.NET Core PasswordHasher hash before running this script.', 1;

IF LEN(@PasswordHash) < 40
    THROW 51011, 'The supplied password hash does not look valid. Generate it using ASP.NET Core PasswordHasher.', 1;

IF EXISTS (SELECT 1 FROM dbo.UserAccount WHERE Username = @Username)
BEGIN
    UPDATE dbo.UserAccount
    SET PasswordHash = @PasswordHash,
        EmployeeId = NULL,
        EncryptedPassword = NULL,
        IsActive = 1
    WHERE Username = @Username;
END
ELSE
BEGIN
    INSERT INTO dbo.UserAccount
        (EmployeeId, Username, PasswordHash, EncryptedPassword, IsActive)
    VALUES
        (NULL, @Username, @PasswordHash, NULL, 1);
END;

PRINT 'Admin account provisioning completed. Verify the account and test login.';
GO
