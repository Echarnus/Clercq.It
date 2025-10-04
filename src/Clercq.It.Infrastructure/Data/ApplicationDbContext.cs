using Microsoft.EntityFrameworkCore;
using Clercq.It.Domain.Entities;

namespace Clercq.It.Infrastructure.Data;

/// <summary>
/// Application database context for Entity Framework Core.
/// 
/// IMPORTANT: This DbContext is configured with MigrationsAssembly for EF Core tooling (dotnet ef) only.
/// Migrations are NEVER executed at runtime in production environments.
/// 
/// Migration execution methods:
/// - Local Development: Clercq.It.Infrastructure.EF.Migrations console project (via Aspire)
/// - Production: SQL scripts executed in deployment pipeline (see .github/workflows/deploy.yml)
/// 
/// DO NOT add Database.Migrate() or Database.EnsureCreated() calls anywhere in the application runtime code.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Blog> Blogs => Set<Blog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}