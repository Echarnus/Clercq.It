using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClercqIt.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory that configures the application for testing.
/// This ensures the app runs in "Test" environment without requiring external services.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set the environment to "Test" so Program.cs uses test authentication
        builder.UseEnvironment("Test");

        base.ConfigureWebHost(builder);
    }
}
