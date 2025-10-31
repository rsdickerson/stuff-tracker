using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StuffTracker.Domain.Data;

public class StuffTrackerDbContextFactory : IDesignTimeDbContextFactory<StuffTrackerDbContext>
{
    public StuffTrackerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StuffTrackerDbContext>();
        
        // Default connection string for design-time operations (migrations)
        // This will be overridden by actual configuration at runtime
        var connectionString = "Server=localhost;Database=stufftracker;User=root;Password=Password12;";
        
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            options => options.MigrationsAssembly("StuffTracker.Api")
        );

        return new StuffTrackerDbContext(optionsBuilder.Options);
    }
}

