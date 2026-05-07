using GithubAnalyzer.WebApi.Entities.Analysis;
using GithubAnalyzer.WebApi.Entities.Auth;
using GithubAnalyzer.WebApi.Entities.Repo;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectQueue> ProjectQueues { get; set; } = null!;
    public DbSet<StatisticAnalysis> StatisticAnalyses { get; set; } = null!;
    public DbSet<CodeGraphAnalysis> CodeGraphAnalyses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>().ToTable("Users", "Auth");
        modelBuilder.Entity<ApplicationRole>().ToTable("Roles", "Auth");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "Auth");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "Auth");

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
        });
    }
}
