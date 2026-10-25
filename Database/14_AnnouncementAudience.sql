/*
  EMPTRACK manual migration: announcement audience targeting.
  Prerequisite: back up the database and verify the current dbo.Announcement and dbo.Department names.
  Existing rows are preserved and receive AudienceType='AllExceptAdmin', which keeps them visible to non-Admin users.
  Apply once during a maintenance window before deploying code that reads these columns. Do not run automatically.
*/
IF COL_LENGTH('dbo.Announcement', 'AudienceType') IS NULL
BEGIN
    ALTER TABLE dbo.Announcement ADD AudienceType nvarchar(30) NOT NULL
        CONSTRAINT DF_Announcement_AudienceType DEFAULT ('AllExceptAdmin') WITH VALUES;
END;
GO
IF COL_LENGTH('dbo.Announcement', 'DepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.Announcement ADD DepartmentId int NULL;
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Announcement_Department_DepartmentId')
BEGIN
    ALTER TABLE dbo.Announcement WITH CHECK ADD CONSTRAINT FK_Announcement_Department_DepartmentId
        FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(DepartmentId);
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Announcement_AudienceType')
BEGIN
    ALTER TABLE dbo.Announcement ADD CONSTRAINT CK_Announcement_AudienceType
        CHECK (AudienceType IN ('AllExceptAdmin', 'Department'));
END;
GO
