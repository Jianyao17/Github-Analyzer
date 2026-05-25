using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GithubAnalyzer.WebApi.Entities.Analysis;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Entities.Cache;
using GithubAnalyzer.WebApi.Entities.Repo;

namespace GithubAnalyzer.WebApi.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectQueue> ProjectQueues { get; set; } = null!;
    public DbSet<StatisticAnalysis> StatisticAnalyses { get; set; } = null!;
    public DbSet<CodeGraphAnalysis> CodeGraphAnalyses { get; set; } = null!;
    
    // Cache tables (schema: Cache)
    public DbSet<CodeGraphCache> CodeGraphCaches { get; set; } = null!;
    public DbSet<StatisticCache> StatisticCaches { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users", "Auth");

            entity.Property(x => x.Id)                  .HasColumnOrder(0);
            entity.Property(x => x.DisplayName)         .HasColumnOrder(1);
            entity.Property(x => x.AvatarUrl)           .HasColumnOrder(2);
            entity.Property(x => x.UserName)            .HasColumnOrder(3);
            entity.Property(x => x.NormalizedUserName)  .HasColumnOrder(4);
            entity.Property(x => x.Email)               .HasColumnOrder(5);
            entity.Property(x => x.NormalizedEmail)     .HasColumnOrder(6);
            entity.Property(x => x.EmailConfirmed)      .HasColumnOrder(7);
            entity.Property(x => x.PasswordHash)        .HasColumnOrder(8);
            entity.Property(x => x.SecurityStamp)       .HasColumnOrder(9);
            entity.Property(x => x.ConcurrencyStamp)    .HasColumnOrder(10);
            entity.Property(x => x.PhoneNumber)         .HasColumnOrder(11);
            entity.Property(x => x.PhoneNumberConfirmed).HasColumnOrder(12);
            entity.Property(x => x.TwoFactorEnabled)    .HasColumnOrder(13);
            entity.Property(x => x.LockoutEnd)          .HasColumnOrder(14);
            entity.Property(x => x.LockoutEnabled)      .HasColumnOrder(15);
            entity.Property(x => x.AccessFailedCount)   .HasColumnOrder(16);
            entity.Property(x => x.CreatedAtUtc)        .HasColumnOrder(17);
            entity.Property(x => x.UpdatedAtUtc)        .HasColumnOrder(18);
        });

        modelBuilder.Entity<ApplicationRole>().ToTable("Roles", "Auth");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "Auth");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "Auth");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "Auth");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "Auth");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "Auth");

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasMany(p => p.Queues)
                .WithOne(q => q.Project)
                .HasForeignKey(q => q.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Statistics)
                .WithOne(sa => sa.Project)
                .HasForeignKey(sa => sa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(p => p.CodeGraphs)
                .WithOne(cg => cg.Project)
                .HasForeignKey(cg => cg.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectQueue>(entity =>
        {
            entity.HasIndex(q => 
                new { q.ProjectId, q.Status, q.Priority, q.JobType });
            
            entity.HasIndex(q => q.ScheduledAtUtc);
            entity.HasIndex(q => q.CompletedAtUtc);
        });

        modelBuilder.Entity<StatisticAnalysis>(entity =>
        {
            entity.HasIndex(sa => sa.ProjectId);
            entity.HasIndex(sa => sa.CommitHash);
            entity.HasIndex(sa => sa.GeneratedAtUtc);
        });

        modelBuilder.Entity<CodeGraphAnalysis>(entity =>
        {
            entity.HasIndex(cg => cg.ProjectId);
            entity.HasIndex(cg => cg.CommitHash);
            entity.HasIndex(cg => cg.GeneratedAtUtc);
            
            entity.Property(cg => cg.GraphJson)
                .HasColumnType("jsonb");
        });

        // ─── Cache schema ────────────────────────────────────────────────
        modelBuilder.Entity<CodeGraphCache>(entity =>
        {
            entity.HasIndex(c => c.LookupKey).IsUnique();
            entity.HasIndex(c => c.CommitHash);

            entity.Property(c => c.GraphJson)
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<StatisticCache>(entity =>
        {
            entity.HasIndex(c => c.LookupKey).IsUnique();
            entity.HasIndex(c => c.CommitHash);
        });
    }
}

