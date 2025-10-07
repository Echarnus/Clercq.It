using Clercq.It.Application;
using Clercq.It.Infrastructure;
using Clercq.It.Api.Features;
using Clercq.It.Api.Features.Auth;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, logging, health checks)
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddOpenApi();

// Add HttpClient for API calls (Auth0, etc.)
builder.Services.AddHttpClient();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "https://localhost:3000",
            "https://www.clercq.it"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Add Authentication - validate JWT tokens from Auth0
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];
if (!string.IsNullOrEmpty(auth0Domain) && !string.IsNullOrEmpty(auth0Audience))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Configure to validate tokens from Auth0
            options.Authority = $"https://{auth0Domain}/";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = $"https://{auth0Domain}/",
                ValidAudience = auth0Audience,
                RoleClaimType = "https://schemas.clercq.it/roles" // Custom claim for roles
            };
            
            // For development, accept tokens from Auth0 without HTTPS requirement
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        });

    builder.Services.AddAuthorization(options =>
    {
        // Fine-grained role-based policies
        options.AddPolicy("AdminView", policy => policy.RequireRole("Admin.View"));
        options.AddPolicy("BlogsContributor", policy => policy.RequireRole("Blogs.Contributor"));
        options.AddPolicy("ProjectsContributor", policy => policy.RequireRole("Projects.Contributor"));
        options.AddPolicy("CertificationsContributor", policy => policy.RequireRole("Certifications.Contributor"));
    });
}

// Register authentication services
builder.Services.AddSingleton<IAuth0Service, Auth0Service>();

// Add Clean Architecture layers
builder.Services.AddApplication();

// Check if we're running under Aspire (has the connection string name)
bool useAspire = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__ClercqItDb"));
if (useAspire)
{
    // Add Aspire PostgreSQL integration
    builder.Services.AddNpgsqlDataSource("ClercqItDb");
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

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowFrontend");

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map feature endpoints
app.MapAuthEndpoints();
app.MapImagesEndpoints();
app.MapProjectsEndpoints();
app.MapBlogsEndpoints();
app.MapCertificationsEndpoints();

app.Run();

// Make the Program class public for testing
public partial class Program { }
