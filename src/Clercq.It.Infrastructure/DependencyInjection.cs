using Amazon.Runtime;
using Amazon.S3;
using Clercq.It.Application.Features.MyZen;
using Clercq.It.Application.Features.Statistics;
using Clercq.It.Domain.Abstractions;
using Clercq.It.Infrastructure.Configuration;
using Clercq.It.Infrastructure.Data;
using Clercq.It.Infrastructure.MyZen;
using Clercq.It.Infrastructure.Repositories;
using Clercq.It.Infrastructure.Services;
using Clercq.It.Infrastructure.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clercq.It.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool useAspirePostgreSQL = false)
    {
        if (useAspirePostgreSQL)
        {
            // DbContext is registered by Aspire's AddNpgsqlDbContext in Program.cs
            // We only register repository dependencies here
        }
        else
        {
            // NOTE: MigrationsAssembly is configured for EF Core tooling (dotnet ef) only.
            // Migrations are NEVER executed at runtime in production.
            // Production migrations are applied via SQL scripts in the deployment pipeline.
            var caCertificate = configuration[DatabaseTls.CaCertificateKey];
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    x => x.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                        .ConfigureDataSource(dataSource => DatabaseTls.Configure(dataSource, caCertificate))));
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
            // Configure AWS S3 client for S3-compatible storage
            // Works with both MinIO (local dev) and Scaleway Object Storage (production)
            var s3Config = new AmazonS3Config
            {
                ServiceURL = objectStorageSettings.Endpoint,
                // UsePathStyle is required for MinIO and recommended for S3-compatible services
                // MinIO: true, Scaleway: true (both use path-style URLs)
                ForcePathStyle = objectStorageSettings.UsePathStyle,
                AuthenticationRegion = objectStorageSettings.Region
            };

            var credentials = new BasicAWSCredentials(
                objectStorageSettings.AccessKey,
                objectStorageSettings.SecretKey);

            services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));
        }

        // Always register the service (even if credentials are missing for tests)
        services.AddScoped<IObjectStorageService, ObjectStorageService>();

        // MyZen: the owner's own MyZen account (issue #345, docs/myzen.md)
        var myZenSection = configuration.GetSection(MyZenOptions.SectionName);
        services.Configure<MyZenOptions>(opts => myZenSection.Bind(opts));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<MyZenSessionLock>();
        services.AddSingleton<IMyZenHttpHandlerFactory, MyZenHttpHandlerFactory>();
        services.AddScoped<IMyZenSessionStore, EfMyZenSessionStore>();
        services.AddScoped<IMyZenGateway, MyZenGateway>();

        // Visitor statistics (issue #429, docs/statistics.md)
        services.AddSingleton<StatisticsState>();
        services.AddScoped<ISiteStatistics, EfSiteStatistics>();

        return services;
    }
}
