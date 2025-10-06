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

// Add Authentication
var jwtSecretKey = builder.Configuration["Authentication:JwtSecretKey"];
if (!string.IsNullOrEmpty(jwtSecretKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Authentication:Issuer"] ?? "Clercq.It",
                ValidAudience = builder.Configuration["Authentication:Audience"] ?? "Clercq.It.Api",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
            };
        });

    builder.Services.AddAuthorization();
}

// Register TokenService
builder.Services.AddSingleton<ITokenService, TokenService>();

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
app.MapProjectsEndpoints();
app.MapBlogsEndpoints();

app.Run();

// Make the Program class public for testing
public partial class Program { }
