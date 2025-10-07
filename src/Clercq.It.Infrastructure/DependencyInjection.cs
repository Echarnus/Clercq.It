using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Infrastructure.Data;
using Clercq.It.Infrastructure.Repositories;
using Clercq.It.Infrastructure.Configuration;
using Clercq.It.Infrastructure.Services;
using Amazon.S3;
using Amazon.Runtime;
using Microsoft.Extensions.Options;

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
        services.AddScoped<ICertificationRepository, CertificationRepository>();

        // Configure Object Storage
        var objectStorageSection = configuration.GetSection("ObjectStorage");
        var objectStorageSettings = new ObjectStorageSettings();
        objectStorageSection.Bind(objectStorageSettings);
        
        services.Configure<ObjectStorageSettings>(opts => objectStorageSection.Bind(opts));
        
        if (!string.IsNullOrEmpty(objectStorageSettings.AccessKey))
        {
            // Configure AWS S3 client for Scaleway Object Storage
            var s3Config = new AmazonS3Config
            {
                ServiceURL = objectStorageSettings.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = objectStorageSettings.Region
            };

            var credentials = new BasicAWSCredentials(
                objectStorageSettings.AccessKey,
                objectStorageSettings.SecretKey);

            services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));
        }
        
        // Always register the service (even if credentials are missing for tests)
        services.AddScoped<IObjectStorageService, ObjectStorageService>();

        return services;
    }
}