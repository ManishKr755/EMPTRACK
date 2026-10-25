/* Manual, non-destructive migration for unique holiday dates.
   Review and back up the database first. This script does not delete or merge rows.
   If duplicates already exist, it stops and requires manual review before rerunning.
   Run after the existing table/constraint scripts and before deploying the updated holiday controller.
*/
IF EXISTS (SELECT [Date] FROM dbo.Holiday GROUP BY [Date] HAVING COUNT(*) > 1)
BEGIN
    THROW 51000, 'Duplicate holiday dates exist. Resolve them manually without deleting required history before applying this constraint.', 1;
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Holiday_Date' AND object_id = OBJECT_ID('dbo.Holiday'))
BEGIN
    CREATE UNIQUE INDEX UX_Holiday_Date ON dbo.Holiday([Date]);
END;
GO
