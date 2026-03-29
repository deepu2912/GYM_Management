using GymManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<MemberMembership> MemberMemberships => Set<MemberMembership>();
    public DbSet<TrainerProfile> TrainerProfiles => Set<TrainerProfile>();
    public DbSet<TrainerSalary> TrainerSalaries => Set<TrainerSalary>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<GymSubscriptionPayment> GymSubscriptionPayments => Set<GymSubscriptionPayment>();
    public DbSet<InvoiceSnapshot> InvoiceSnapshots => Set<InvoiceSnapshot>();
    public DbSet<MemberAttendance> MemberAttendances => Set<MemberAttendance>();
    public DbSet<GymProfile> GymProfiles => Set<GymProfile>();
    public DbSet<GymTenant> GymTenants => Set<GymTenant>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<SuperAdminInvoiceSettings> SuperAdminInvoiceSettings => Set<SuperAdminInvoiceSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Email)
            .IsUnique();
        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.GymTenantId);
        modelBuilder.Entity<AppUser>()
            .HasOne(x => x.GymTenant)
            .WithMany()
            .HasForeignKey(x => x.GymTenantId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Member>()
            .HasIndex(x => x.Email)
            .IsUnique();
        modelBuilder.Entity<Member>()
            .HasIndex(x => x.GymTenantId);
        modelBuilder.Entity<Member>()
            .HasIndex(x => x.JoiningDate);
        modelBuilder.Entity<Member>()
            .HasOne<GymTenant>()
            .WithMany()
            .HasForeignKey(x => x.GymTenantId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AppUser>()
            .Property(x => x.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Member>()
            .Property(x => x.Gender)
            .HasConversion<string>();

        modelBuilder.Entity<Member>()
            .Property(x => x.MembershipStatus)
            .HasConversion<string>();

        modelBuilder.Entity<MembershipPlan>()
            .HasIndex(x => new { x.GymTenantId, x.PlanName })
            .IsUnique();
        modelBuilder.Entity<MembershipPlan>()
            .HasIndex(x => x.GymTenantId);
        modelBuilder.Entity<MembershipPlan>()
            .HasOne<GymTenant>()
            .WithMany()
            .HasForeignKey(x => x.GymTenantId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MembershipPlan>()
            .Property(x => x.Duration)
            .HasConversion<string>();

        modelBuilder.Entity<MembershipPlan>()
            .Property(x => x.MembershipType)
            .HasConversion<string>();

        modelBuilder.Entity<MemberMembership>()
            .HasOne(x => x.Member)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MemberMembership>()
            .HasIndex(x => new { x.MemberId, x.StartDate, x.EndDate });
        modelBuilder.Entity<MemberMembership>()
            .HasIndex(x => new { x.SecondaryMemberId, x.StartDate, x.EndDate });

        modelBuilder.Entity<MemberMembership>()
            .HasOne(x => x.SecondaryMember)
            .WithMany()
            .HasForeignKey(x => x.SecondaryMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MemberMembership>()
            .HasOne(x => x.MembershipPlan)
            .WithMany(x => x.MemberMemberships)
            .HasForeignKey(x => x.MembershipPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TrainerProfile>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<TrainerProfile>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainerSalary>()
            .HasIndex(x => new { x.TrainerUserId, x.Year, x.Month })
            .IsUnique();

        modelBuilder.Entity<TrainerSalary>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.TrainerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payment>()
            .HasIndex(x => x.InvoiceNumber)
            .IsUnique(false);

        modelBuilder.Entity<Payment>()
            .HasIndex(x => x.ReceiptNumber)
            .IsUnique();

        modelBuilder.Entity<Payment>()
            .Property(x => x.PaymentMode)
            .HasConversion<string>();

        modelBuilder.Entity<Payment>()
            .HasOne<Member>()
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GymSubscriptionPayment>()
            .HasIndex(x => x.InvoiceNumber)
            .IsUnique();
        modelBuilder.Entity<GymSubscriptionPayment>()
            .HasIndex(x => x.GymTenantId);
        modelBuilder.Entity<GymSubscriptionPayment>()
            .HasOne<GymTenant>()
            .WithMany()
            .HasForeignKey(x => x.GymTenantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceSnapshot>()
            .HasIndex(x => x.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<InvoiceSnapshot>()
            .HasOne<Member>()
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MemberAttendance>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<MemberAttendance>()
            .HasIndex(x => new { x.MemberId, x.AttendanceDate })
            .IsUnique();

        modelBuilder.Entity<MemberAttendance>()
            .HasOne<Member>()
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GymProfile>()
            .HasIndex(x => x.Email)
            .IsUnique();
        modelBuilder.Entity<GymProfile>()
            .HasIndex(x => x.GymTenantId);
        modelBuilder.Entity<GymProfile>()
            .HasOne<GymTenant>()
            .WithMany()
            .HasForeignKey(x => x.GymTenantId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GymTenant>()
            .HasIndex(x => x.GymName)
            .IsUnique();

        modelBuilder.Entity<GymTenant>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<SubscriptionPlan>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<SuperAdminInvoiceSettings>()
            .HasIndex(x => x.Email)
            .IsUnique(false);
    }
}
