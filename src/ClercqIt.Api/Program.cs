using System.Text;
using System.Threading.RateLimiting;
using Clercq.It.Api.Features;
using Clercq.It.Api.Features.Auth;
using Clercq.It.Application;
using Clercq.It.Infrastructure;
using ClercqIt.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, logging, health checks)
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddOpenApi();

// Add HttpClient for API calls (Cloud IAM, etc.)
builder.Services.AddHttpClient();

// Bank-grade security: Add rate limiting to prevent DDoS attacks
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // API endpoints rate limit: 30 requests per minute
    options.AddPolicy("api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Authentication endpoints: stricter limit (10 requests per minute)
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.RejectionStatusCode = 429; // Too Many Requests
});

// Add CORS for frontend
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Determine Keycloak URL from Aspire service reference or configuration
var keycloakServiceUrl = builder.Configuration["services:keycloak:http:0"];  // Aspire local
var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"];             // External Keycloak
var effectiveKeycloakUrl = keycloakServiceUrl ?? keycloakBaseUrl;
bool useKeycloak = !string.IsNullOrEmpty(effectiveKeycloakUrl);

// Check if running in test environment (WebApplicationFactory sets this)
bool isTestEnvironment = builder.Environment.EnvironmentName == "Test" ||
    builder.Configuration["ASPNETCORE_TEST_CONTENTROOT_CLERCQIT_API"] != null;

if (isTestEnvironment)
{
    // Test environment: Use minimal auth that doesn't require external services
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false
            };
        });

    // Register a mock auth service for tests
    builder.Services.AddSingleton<ICloudIAMAuthService, LocalKeycloakAuthService>();
}
else if (useKeycloak)
{
    // Keycloak authentication (local Aspire or external)
    var keycloakRealm = builder.Configuration["Keycloak:Realm"] ?? "clercqit";
    var keycloakAuthority = $"{effectiveKeycloakUrl}/realms/{keycloakRealm}";
    var isLocalKeycloak = !string.IsNullOrEmpty(keycloakServiceUrl);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakAuthority;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = keycloakAuthority,
                RoleClaimType = "roles" // Use our custom roles claim
            };
            // Only disable HTTPS for local development Keycloak
            options.RequireHttpsMetadata = !isLocalKeycloak;
        });

    // Register Keycloak auth service
    builder.Services.AddSingleton<ICloudIAMAuthService, LocalKeycloakAuthService>();

    // Log which Keycloak is being used
    Console.WriteLine($"[Auth] Using {(isLocalKeycloak ? "local" : "external")} Keycloak at: {effectiveKeycloakUrl}");
}
else
{
    // Fallback: Use Cloud IAM (custom auth API)
    builder.Services.AddOptions<CloudIAMSettings>()
        .BindConfiguration(CloudIAMSettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var cloudIAMSettings = builder.Configuration
        .GetSection(CloudIAMSettings.SectionName)
        .Get<CloudIAMSettings>() ?? throw new InvalidOperationException(
            "Authentication configuration is missing. Either configure Keycloak:BaseUrl for Keycloak, " +
            "or CloudIAM settings for custom authentication. See docs/ciam.md for instructions.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = cloudIAMSettings.ApiUrl;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = cloudIAMSettings.ApiUrl,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };

            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
            else
            {
                options.RequireHttpsMetadata = true;
            }
        });

    // Register Cloud IAM auth service
    builder.Services.AddSingleton<ICloudIAMAuthService, CloudIAMAuthService>();
}

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    // Fine-grained role-based policies
    options.AddPolicy("AdminView", policy => policy.RequireRole("Admin.View"));
    options.AddPolicy("BlogsContributor", policy => policy.RequireRole("Blogs.Contributor"));
    options.AddPolicy("ProjectsContributor", policy => policy.RequireRole("Projects.Contributor"));
    options.AddPolicy("CertificationsContributor", policy => policy.RequireRole("Certifications.Contributor"));
});

// Add Clean Architecture layers
builder.Services.AddApplication();

// Check if we're running under Aspire (connection string is set by Aspire orchestration)
var aspireConnectionString = builder.Configuration.GetConnectionString("ClercqItDb");
bool useAspire = !string.IsNullOrEmpty(aspireConnectionString);
if (useAspire)
{
    // Add Aspire PostgreSQL EF Core integration directly
    builder.AddNpgsqlDbContext<Clercq.It.Infrastructure.Data.ApplicationDbContext>("ClercqItDb");
    builder.Services.AddInfrastructure(builder.Configuration, useAspirePostgreSQL: true);
}
else
{
    // Fallback to traditional connection string
    builder.Services.AddInfrastructure(builder.Configuration, useAspirePostgreSQL: false);
}

var app = builder.Build();

// IMPORTANT: Migrations are NOT executed at runtime.
// - Local Development: Migrations run via Clercq.It.Infrastructure.EF.Migrations console project (Aspire orchestration)
// - Production: Migrations applied via SQL scripts in deployment pipeline (.github/workflows/deploy.yml)
// DO NOT add app.Services.GetRequiredService<DbContext>().Database.Migrate() or similar code here.

// Configure the HTTP request pipeline
app.MapDefaultEndpoints(); // Aspire health checks and telemetry

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Bank-grade security: Add security headers middleware
app.Use(async (context, next) =>
{
    // Remove server header for security through obscurity
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Remove("X-Powered-By");

    // Add security headers using indexer to avoid warnings
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    if (!app.Environment.IsDevelopment())
    {
        // HSTS - Force HTTPS for 1 year in production
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    }

    // Content Security Policy for API
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

    await next();
});

// Only use HTTPS redirection in production (Aspire uses HTTP internally)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Use CORS
app.UseCors("AllowFrontend");

// Bank-grade security: Enable rate limiting
app.UseRateLimiter();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map feature endpoints with rate limiting
app.MapAuthEndpoints();
app.MapImagesEndpoints();
app.MapProjectsEndpoints();
app.MapBlogsEndpoints();
app.MapCertificationsEndpoints();

app.Run();

// Make the Program class public for testing
public partial class Program { }
