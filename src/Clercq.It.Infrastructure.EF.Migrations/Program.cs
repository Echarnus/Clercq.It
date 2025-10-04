using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Clercq.It.Infrastructure.Data;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("Error: Connection string 'DefaultConnection' not found.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseNpgsql(connectionString);

try
{
    using var context = new ApplicationDbContext(optionsBuilder.Options);
    
    Console.WriteLine("Applying database migrations...");
    await context.Database.MigrateAsync();
    
    Console.WriteLine("✅ Database migrations applied successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"❌ Error applying migrations: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}
