using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Clercq.It.Infrastructure.Data;

namespace Clercq.It.Infrastructure.EF;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = "Host=localhost;Port=5432;Database=ClercqItDb;Username=clercq_user;Password=clercq_pass;";
        
        optionsBuilder.UseNpgsql(connectionString, 
            x => x.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
