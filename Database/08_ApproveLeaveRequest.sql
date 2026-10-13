/*
    ============================================================
    EMPTRACK - Leave Approval Workflow
    File: 08_ApproveLeaveRequest.sql
    Purpose: Safely approve a pending leave request and deduct
             the employee's leave balance in one transaction.

    IMPORTANT:
    - Does not create or alter tables.
    - Uses the existing LeaveRequest and LeaveBalance tables.
    - ApprovedByUserId stores UserAccount.UserAccountId.
    - The application performs approval authorization before
      calling this procedure.
    ============================================================
*/

USE EMPTRACK;
GO

CREATE OR ALTER PROCEDURE dbo.ApproveLeaveRequest
    @LeaveRequestId INT,
    @ApprovedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @EmployeeId INT,
        @LeaveTypeId INT,
        @StartDate DATE,
        @EndDate DATE,
        @Status NVARCHAR(20),
        @NumberOfDays INT,
        @AvailableLeaves DECIMAL(5,2);

    BEGIN TRY
        BEGIN TRANSACTION;

        /* ---------------------------------------------
           1. Verify the approver account exists/active.
           --------------------------------------------- */
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.UserAccount
            WHERE UserAccountId = @ApprovedByUserId
              AND IsActive = 1
        )
        BEGIN
            THROW 51001, 'The approver account could not be found.', 1;
        END;


        /* ---------------------------------------------
           2. Lock the request while it is being approved.
              This prevents a second approval/rejection
              from changing the same request at the same time.
           --------------------------------------------- */
        SELECT
            @EmployeeId = EmployeeId,
            @LeaveTypeId = LeaveTypeId,
            @StartDate = StartDate,
            @EndDate = EndDate,
            @Status = Status
        FROM dbo.LeaveRequest WITH (UPDLOCK, HOLDLOCK)
        WHERE LeaveRequestId = @LeaveRequestId;


        IF @EmployeeId IS NULL
        BEGIN
            THROW 51002, 'Leave request was not found.', 1;
        END;


        /* ---------------------------------------------
           3. Only Pending requests can be approved.
           --------------------------------------------- */
        IF @Status <> N'Pending'
        BEGIN
            THROW 51003, 'This leave request is not pending.', 1;
        END;


        /* ---------------------------------------------
           4. Calculate the requested number of days.
           --------------------------------------------- */
        SET @NumberOfDays =
            DATEDIFF(DAY, @StartDate, @EndDate) + 1;

        IF @NumberOfDays <= 0
        BEGIN
            THROW 51004, 'The leave request has an invalid date range.', 1;
        END;


        /* ---------------------------------------------
           5. Lock the balance row.

              UPDLOCK + HOLDLOCK is important here.
              If two requests use the same balance row,
              only one transaction can check/deduct it
              at a time.
           --------------------------------------------- */
        SELECT
            @AvailableLeaves = AvailableLeaves
        FROM dbo.LeaveBalance WITH (UPDLOCK, HOLDLOCK)
        WHERE EmployeeId = @EmployeeId
          AND LeaveTypeId = @LeaveTypeId;


        IF @AvailableLeaves IS NULL
        BEGIN
            THROW 51005, 'The leave balance could not be found.', 1;
        END;


        /* ---------------------------------------------
           6. Check the balance before deduction.
           --------------------------------------------- */
        IF @AvailableLeaves < @NumberOfDays
        BEGIN
            THROW 51006, 'The employee does not have enough available leave.', 1;
        END;


        /* ---------------------------------------------
           7. Deduct the requested leave.
           --------------------------------------------- */
        UPDATE dbo.LeaveBalance
        SET AvailableLeaves =
            AvailableLeaves - @NumberOfDays
        WHERE EmployeeId = @EmployeeId
          AND LeaveTypeId = @LeaveTypeId;


        /* ---------------------------------------------
           8. Mark the request as approved.
           --------------------------------------------- */
        UPDATE dbo.LeaveRequest
        SET
            Status = N'Approved',
            ApprovedByUserId = @ApprovedByUserId,
            ApprovedAt = SYSDATETIME()
        WHERE LeaveRequestId = @LeaveRequestId
          AND Status = N'Pending';


        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 51007, 'This leave request is no longer pending.', 1;
        END;


        /* ---------------------------------------------
           9. Everything succeeded.
           --------------------------------------------- */
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;
END;
GO
