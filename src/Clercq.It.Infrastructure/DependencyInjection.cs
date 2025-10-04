using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Infrastructure.Data;
using Clercq.It.Infrastructure.Repositories;

namespace Clercq.It.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool useAspirePostgreSQL = false)
    {
        if (useAspirePostgreSQL)
        {
            // This will be configured by Aspire's AddNpgsqlDataSource
            // NOTE: MigrationsAssembly is configured for EF Core tooling (dotnet ef) only.
            // Migrations are NEVER executed at runtime in production.
            // Production migrations are applied via SQL scripts in the deployment pipeline.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(x => x.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        }
        else
        {
            // NOTE: MigrationsAssembly is configured for EF Core tooling (dotnet ef) only.
            // Migrations are NEVER executed at runtime in production.
            // Production migrations are applied via SQL scripts in the deployment pipeline.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    x => x.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IBlogRepository, BlogRepository>();

        return services;
    }
}