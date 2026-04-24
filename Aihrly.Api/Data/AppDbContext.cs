using Aihrly.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationNote> ApplicationNotes => Set<ApplicationNote>();
    public DbSet<StageHistory> StageHistories => Set<StageHistory>();
    public DbSet<ApplicationScore> ApplicationScores => Set<ApplicationScore>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // store enums as strings so the DB is readable without code context
        modelBuilder.Entity<TeamMember>().Property(t => t.Role).HasConversion<string>();
        modelBuilder.Entity<Job>().Property(j => j.Status).HasConversion<string>();
        modelBuilder.Entity<Application>().Property(a => a.CurrentStage).HasConversion<string>();
        modelBuilder.Entity<ApplicationNote>().Property(n => n.Type).HasConversion<string>();
        modelBuilder.Entity<ApplicationScore>().Property(s => s.Dimension).HasConversion<string>();
        modelBuilder.Entity<StageHistory>().Property(s => s.FromStage).HasConversion<string>();
        modelBuilder.Entity<StageHistory>().Property(s => s.ToStage).HasConversion<string>();

        // one row per dimension per application -- PUT semantics
        modelBuilder.Entity<ApplicationScore>()
            .HasIndex(s => new { s.ApplicationId, s.Dimension })
            .IsUnique();

        // prevent duplicate applications from the same candidate
        modelBuilder.Entity<Application>()
            .HasIndex(a => new { a.JobId, a.CandidateEmail })
            .IsUnique();

        // these indexes back the queries we run most often
        modelBuilder.Entity<ApplicationNote>().HasIndex(n => n.ApplicationId);
        modelBuilder.Entity<StageHistory>().HasIndex(s => s.ApplicationId);
        modelBuilder.Entity<ApplicationScore>().HasIndex(s => s.ApplicationId);

        // restrict instead of cascade so we never silently delete audit data
        modelBuilder.Entity<ApplicationNote>()
            .HasOne(n => n.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StageHistory>()
            .HasOne(s => s.ChangedBy).WithMany().OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationScore>()
            .HasOne(s => s.SetBy).WithMany().OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationScore>()
            .HasOne(s => s.UpdatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);

        SeedTeamMembers(modelBuilder);
    }

    private static void SeedTeamMembers(ModelBuilder modelBuilder)
    {
        // fixed GUIDs so the README can reference them and tests can rely on them
        modelBuilder.Entity<TeamMember>().HasData(
            new TeamMember
            {
                Id = new Guid("a1b2c3d4-0001-0000-0000-000000000000"),
                Name = "Alice Johnson",
                Email = "alice@aihrly.com",
                Role = TeamMemberRole.Recruiter
            },
            new TeamMember
            {
                Id = new Guid("a1b2c3d4-0002-0000-0000-000000000000"),
                Name = "Bob Martinez",
                Email = "bob@aihrly.com",
                Role = TeamMemberRole.HiringManager
            },
            new TeamMember
            {
                Id = new Guid("a1b2c3d4-0003-0000-0000-000000000000"),
                Name = "Carol Chen",
                Email = "carol@aihrly.com",
                Role = TeamMemberRole.Recruiter
            }
        );
    }
}
