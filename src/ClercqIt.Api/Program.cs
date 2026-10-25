using System.Threading.RateLimiting;
using Clercq.It.Api;
using Clercq.It.Api.FeatureFlags;
using Clercq.It.Api.Features;
using Clercq.It.Api.Features.Auth;
using Clercq.It.Api.Features.Mcp;
using Clercq.It.Application;
using Clercq.It.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

if (useKeycloak)
{
    // Keycloak authentication (local Aspire or external)
    var keycloakRealm = builder.Configuration["Keycloak:Realm"] ?? "clercqit";
    var keycloakAuthority = $"{effectiveKeycloakUrl}/realms/{keycloakRealm}";
    var isLocalKeycloak = !string.IsNullOrEmpty(keycloakServiceUrl);

    void ConfigureBearer(JwtBearerOptions options)
    {
        options.Authority = keycloakAuthority;
        // Keep claim names as Keycloak sends them, so RoleClaimType "roles" matches
        // (the default inbound map renames "roles" and every role policy would fail).
        options.MapInboundClaims = false;
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
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(ConfigureBearer)
        .AddMcpAuthentication(ConfigureBearer, keycloakAuthority, builder.Configuration["Mcp:PublicBaseUrl"]);

    // The admin UI's authorization code + PKCE settings (GET /api/auth/config)
    builder.Services.AddSingleton(new KeycloakClientSettings(
        keycloakAuthority, builder.Configuration["Keycloak:ClientId"] ?? "clercqit-web"));

    // What the admin Settings page shows about the identity provider (GET /api/auth/identity-provider)
    builder.Services.AddSingleton(IdentityProviderSettings.Create(
        effectiveKeycloakUrl!, keycloakRealm, isLocalKeycloak,
        builder.Configuration["Keycloak:ClientId"] ?? "clercqit-web",
        builder.Configuration["Keycloak:McpClientId"] ?? "clercqit-mcp"));

    // Log which Keycloak is being used
    Console.WriteLine($"[Auth] Using {(isLocalKeycloak ? "local" : "external")} Keycloak at: {effectiveKeycloakUrl}");
}
else
{
    throw new InvalidOperationException(
        "Authentication is not configured. Set Keycloak:BaseUrl (and optionally Keycloak:Realm) " +
        "to the Cloud IAM or local Keycloak URL. See docs/ciam.md.");
}

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    // Fine-grained role-based policies
    options.AddPolicy("AdminView", policy => policy.RequireRole("Admin.View"));
    options.AddPolicy("BlogsContributor", policy => policy.RequireRole("Blogs.Contributor"));
    options.AddPolicy("ProjectsContributor", policy => policy.RequireRole("Projects.Contributor"));
    options.AddPolicy("CertificationsContributor", policy => policy.RequireRole("Certifications.Contributor"));
    // The admin pages and what only they need (session check, feature flags): any admin role (issue #454)
    options.AddPolicy(AuthEndpoints.AdminAreaPolicy, policy => policy
        .RequireRole("Admin.View", "Blogs.Contributor", "Projects.Contributor", "Certifications.Contributor"));
    // MyZen reaches the owner's own MyZen account, so it has its own role (docs/myzen.md)
    options.AddPolicy(MyZenEndpoints.OwnerPolicy, policy => policy.RequireRole("MyZen.Owner"));
    // MCP tools have no endpoint filter, so the flag is part of their policy
    options.AddPolicy(MyZenTools.Policy, policy => policy
        .RequireRole("MyZen.Owner")
        .AddRequirements(new FeatureFlagRequirement(MyZenEndpoints.FeatureFlag)));
    // The MCP server: any content contributor or the MyZen owner; each tool then checks its own role (docs/mcp.md)
    options.AddPolicy(McpEndpoints.Policy, policy => policy
        .AddAuthenticationSchemes(McpEndpoints.BearerScheme)
        .RequireRole("Projects.Contributor", "Certifications.Contributor", "Blogs.Contributor", "MyZen.Owner"));
});

// Add Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddFeatureFlags();

// Validation failures from the MediatR pipeline become 400 responses
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<MyZenExceptionHandler>();
builder.Services.AddProblemDetails();

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

builder.Services.AddHostedService<MyZenKeepAliveService>();
builder.Services.AddContentMcpServer();

var app = builder.Build();

// IMPORTANT: Migrations are NOT executed at runtime.
// - Local Development: Migrations run via Clercq.It.Infrastructure.EF.Migrations console project (Aspire orchestration)
// - Production: Migrations applied via SQL scripts in deployment pipeline (.github/workflows/deploy.yml)
// DO NOT add app.Services.GetRequiredService<DbContext>().Database.Migrate() or similar code here.

// Configure the HTTP request pipeline
app.UseExceptionHandler();
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
app.MapMyZenEndpoints();
app.MapStatisticsEndpoints();
app.MapFeatureFlagEndpoints();
app.MapContentMcp();

app.Run();

// Make the Program class public for testing
public partial class Program { }
