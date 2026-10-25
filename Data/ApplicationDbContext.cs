using EMPTRACK.Models;
using Microsoft.EntityFrameworkCore;
using TaskEntity = EMPTRACK.Models.Task;

namespace EMPTRACK.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<SalaryPayment> SalaryPayments => Set<SalaryPayment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Concern> Concerns => Set<Concern>();
    public DbSet<ConcernMessage> ConcernMessages => Set<ConcernMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        // Employee
        // ============================================================

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee");

            entity.HasKey(e => e.EmployeeId);

            entity.Property(e => e.EmployeeId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeCode)
                .IsRequired()
                .HasMaxLength(6)
                .IsFixedLength();

            entity.Property(e => e.EmployeeSerial)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .IsRequired();

            entity.Property(e => e.LastName)
                .IsRequired();

            entity.Property(e => e.Email)
                .IsRequired();

            entity.Property(e => e.Phone)
                .IsRequired(false);

            entity.Property(e => e.Role)
                .IsRequired();

            entity.Property(e => e.TeamId)
                .IsRequired(false);

            entity.Property(e => e.DepartmentId)
                .IsRequired(false);

            entity.Property(e => e.JoiningDate)
                .IsRequired();

            entity.Property(e => e.EmploymentStatus)
                .IsRequired();

            entity.HasIndex(e => e.EmployeeCode)
                .IsUnique();

            entity.HasIndex(e => e.EmployeeSerial)
                .IsUnique();

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasOne(e => e.Team)
                .WithMany(t => t.Employees)
                .HasForeignKey(e => e.TeamId)
                .IsRequired(false);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .IsRequired(false);
        });

        // ============================================================
        // UserAccount
        // ============================================================

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("UserAccount");

            entity.HasKey(e => e.UserAccountId);

            entity.Property(e => e.UserAccountId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeId)
                .IsRequired(false);

            entity.Property(e => e.Username)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .IsRequired();

            entity.Property(e => e.EncryptedPassword)
                .IsRequired(false)
                .HasMaxLength(2048);

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.HasIndex(e => e.Username)
                .IsUnique();

            entity.HasIndex(e => e.EmployeeId)
                .IsUnique()
                .HasFilter("[EmployeeId] IS NOT NULL");

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.UserAccounts)
                .HasForeignKey(e => e.EmployeeId)
                .IsRequired(false);
        });

        // ============================================================
        // Department
        // ============================================================

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Department");

            entity.HasKey(e => e.DepartmentId);

            entity.Property(e => e.DepartmentId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Code)
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired();

            entity.Property(e => e.HeadManagerEmployeeId)
                .IsRequired(false);

            entity.Property(e => e.HeadHREmployeeId)
                .IsRequired(false);

            entity.HasIndex(e => e.Code)
                .IsUnique();

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasOne(e => e.HeadManager)
                .WithMany()
                .HasForeignKey(e => e.HeadManagerEmployeeId)
                .IsRequired(false);

            entity.HasOne(e => e.HeadHREmployee)
                .WithMany()
                .HasForeignKey(e => e.HeadHREmployeeId)
                .IsRequired(false);

            entity.HasMany(e => e.Teams)
                .WithOne(e => e.Department)
                .HasForeignKey(e => e.DepartmentId);
        });

        // ============================================================
        // Team
        // ============================================================

        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("Team");

            entity.HasKey(e => e.TeamId);

            entity.Property(e => e.TeamId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.DepartmentId)
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired();

            entity.Property(e => e.ManagerEmployeeId)
                .IsRequired();

            entity.HasIndex(e => e.ManagerEmployeeId)
                .IsUnique();

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Teams)
                .HasForeignKey(e => e.DepartmentId);

            entity.HasOne(e => e.ManagerEmployee)
                .WithMany()
                .HasForeignKey(e => e.ManagerEmployeeId)
                .IsRequired();
        });

        // ============================================================
        // Attendance
        // ============================================================

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("Attendance");

            entity.HasKey(e => e.AttendanceId);

            entity.Property(e => e.AttendanceId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeId)
                .IsRequired();

            entity.Property(e => e.Date)
                .IsRequired();

            entity.Property(e => e.CheckInTime)
                .IsRequired();

            entity.Property(e => e.CheckOutTime)
                .IsRequired(false);

            entity.HasIndex(e => new
            {
                e.EmployeeId,
                e.Date
            }).IsUnique();

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(e => e.EmployeeId);
        });

        // ============================================================
        // LeaveType
        // ============================================================

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.ToTable("LeaveType");

            entity.HasKey(e => e.LeaveTypeId);

            entity.Property(e => e.LeaveTypeId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .IsRequired();

            entity.Property(e => e.AllocationType)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.AllocationLimit)
                .HasPrecision(5, 2)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        // ============================================================
        // LeaveBalance
        // ============================================================

        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.ToTable("LeaveBalance");

            entity.HasKey(e => e.LeaveBalanceId);

            entity.Property(e => e.LeaveBalanceId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeId)
                .IsRequired();

            entity.Property(e => e.LeaveTypeId)
                .IsRequired();

            entity.Property(e => e.AvailableLeaves)
                .IsRequired();

            entity.Property(e => e.LastAccruedPeriod)
                .IsRequired(false);

            entity.HasIndex(e => new
            {
                e.EmployeeId,
                e.LeaveTypeId
            }).IsUnique();

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.LeaveBalances)
                .HasForeignKey(e => e.EmployeeId);

            entity.HasOne(e => e.LeaveType)
                .WithMany(e => e.LeaveBalances)
                .HasForeignKey(e => e.LeaveTypeId);
        });

        // ============================================================
        // LeaveRequest
        // ============================================================

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("LeaveRequest");

            entity.HasKey(e => e.LeaveRequestId);

            entity.Property(e => e.LeaveRequestId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeId)
                .IsRequired();

            entity.Property(e => e.LeaveTypeId)
                .IsRequired();

            entity.Property(e => e.StartDate)
                .IsRequired();

            entity.Property(e => e.EndDate)
                .IsRequired();

            entity.Property(e => e.Reason)
                .IsRequired(false)
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.ApprovedByUserId)
                .IsRequired(false);

            entity.Property(e => e.ApprovedAt)
                .IsRequired(false);

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(e => e.EmployeeId);

            entity.HasOne(e => e.LeaveType)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(e => e.LeaveTypeId);

            entity.HasOne(e => e.ApprovedByUser)
                .WithMany(e => e.ApprovedLeaveRequests)
                .HasForeignKey(e => e.ApprovedByUserId)
                .IsRequired(false);
        });

        // ============================================================
        // Task
        // ============================================================

        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.ToTable("Task");

            entity.HasKey(e => e.TaskId);

            entity.Property(e => e.TaskId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .IsRequired();

            entity.Property(e => e.Description)
                .IsRequired();

            entity.Property(e => e.AssignedEmployeeId)
                .IsRequired();

            entity.Property(e => e.CreatedByEmployeeId)
                .IsRequired();

            entity.Property(e => e.DueDate)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.Priority)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(e => e.AssignedEmployee)
                .WithMany(e => e.AssignedTasks)
                .HasForeignKey(e => e.AssignedEmployeeId)
                .IsRequired();

            entity.HasOne(e => e.CreatedByEmployee)
                .WithMany(e => e.CreatedTasks)
                .HasForeignKey(e => e.CreatedByEmployeeId)
                .IsRequired();
        });

        // ============================================================
        // PerformanceReview
        // ============================================================

        modelBuilder.Entity<PerformanceReview>(entity =>
        {
            entity.ToTable("PerformanceReview");

            entity.HasKey(e => e.ReviewId);

            entity.Property(e => e.ReviewId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeId)
                .IsRequired();

            entity.Property(e => e.ReviewerEmployeeId)
                .IsRequired();

            entity.Property(e => e.ReviewDate)
                .IsRequired();

            entity.Property(e => e.Rating)
                .IsRequired();

            entity.Property(e => e.Feedback)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.Reviews)
                .HasForeignKey(e => e.EmployeeId)
                .IsRequired();

            entity.HasOne(e => e.Reviewer)
                .WithMany(e => e.ReviewsGiven)
                .HasForeignKey(e => e.ReviewerEmployeeId)
                .IsRequired();
        });

        // ============================================================
        // SalaryPayment
        // ============================================================

        modelBuilder.Entity<SalaryPayment>(entity =>
        {
            entity.ToTable("SalaryPayment");

            entity.HasKey(e => e.PaymentId);

            entity.Property(e => e.PaymentId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeId)
                .IsRequired();

            entity.Property(e => e.PaymentDate)
                .IsRequired();

            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.SalaryPayments)
                .HasForeignKey(e => e.EmployeeId);
        });

        // ============================================================
        // Notification
        // ============================================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");

            entity.HasKey(e => e.NotificationId);

            entity.Property(e => e.NotificationId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeId)
                .IsRequired();

            entity.Property(e => e.Title)
                .IsRequired();

            entity.Property(e => e.Message)
                .IsRequired();

            entity.Property(e => e.IsRead)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.Notifications)
                .HasForeignKey(e => e.EmployeeId);
        });

        // ============================================================
        // Announcement
        // ============================================================

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.ToTable("Announcement");

            entity.HasKey(e => e.AnnouncementId);

            entity.Property(e => e.AnnouncementId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .IsRequired();

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.PublishedAt)
                .IsRequired();

            entity.Property(e => e.AudienceType)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("AllExceptAdmin");

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================================
        // Holiday
        // ============================================================

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.ToTable("Holiday");

            entity.HasKey(e => e.HolidayId);

            entity.Property(e => e.HolidayId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .IsRequired();

            entity.Property(e => e.Date)
                .IsRequired();

            entity.HasIndex(e => e.Date).IsUnique();
        });

        // ============================================================
        // Concern
        // ============================================================

        modelBuilder.Entity<Concern>(entity =>
        {
            entity.ToTable("Concern");

            entity.HasKey(e => e.ConcernId);

            entity.Property(e => e.ConcernId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.EmployeeId)
                .IsRequired();

            entity.Property(e => e.Subject)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.Concerns)
                .HasForeignKey(e => e.EmployeeId);
        });

        // ============================================================
        // ConcernMessage
        // ============================================================

        modelBuilder.Entity<ConcernMessage>(entity =>
        {
            entity.ToTable("ConcernMessage");

            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.MessageId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ConcernId)
                .IsRequired();

            entity.Property(e => e.SenderEmployeeId)
                .IsRequired();

            entity.Property(e => e.Message)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(e => e.Concern)
                .WithMany(e => e.Messages)
                .HasForeignKey(e => e.ConcernId)
                .IsRequired();

            entity.HasOne(e => e.SenderEmployee)
                .WithMany(e => e.ConcernMessages)
                .HasForeignKey(e => e.SenderEmployeeId)
                .IsRequired();
        });

    }
}