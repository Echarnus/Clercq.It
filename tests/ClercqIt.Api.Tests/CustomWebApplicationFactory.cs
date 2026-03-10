using Clercq.It.Domain.Abstractions;
using Clercq.It.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace ClercqIt.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory that configures the application for integration testing.
/// Replaces the real PostgreSQL database with an EF Core InMemory provider,
/// replaces the real object storage service with a mock implementation,
/// and configures JWT authentication to accept test tokens.
/// Each factory instance uses a unique database name to prevent cross-test pollution.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove ALL Entity Framework related service descriptors to avoid
            // the "multiple database providers registered" conflict between
            // Npgsql (registered by the app) and InMemory (registered by tests).
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true ||
                    d.ImplementationType?.FullName?.Contains("EntityFrameworkCore") == true ||
                    d.ImplementationType?.FullName?.Contains("Npgsql") == true ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)))
                .ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            // Register InMemory database with a unique name per factory instance
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Replace IObjectStorageService with a mock
            var objectStorageDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IObjectStorageService));
            if (objectStorageDescriptor != null)
            {
                services.Remove(objectStorageDescriptor);
            }

            var mockObjectStorage = new Mock<IObjectStorageService>();
            mockObjectStorage
                .Setup(s => s.UploadFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://test-storage.example.com/test-image.png");

            mockObjectStorage
                .Setup(s => s.DeleteFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(mockObjectStorage.Object);

            // Reconfigure JWT Bearer authentication to accept test tokens.
            // The app's Test environment already disables most validation, but we need
            // to provide the signing key so the handler can decode our test tokens.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    IssuerSigningKey = TestJwtTokenHelper.SecurityKey,
                    RoleClaimType = "roles"
                };
            });
        });

        base.ConfigureWebHost(builder);
    }
}
