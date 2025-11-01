using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using StuffTracker.Api;
using StuffTracker.Api.Data;
using StuffTracker.Domain.Data;

namespace StuffTracker.Tests.Integration;

/// <summary>
/// Test fixture that provides an in-memory test server with test database isolation.
/// Uses a separate test database (stufftracker_test) to avoid conflicts with development data.
/// </summary>
public class GraphQLTestFixture : WebApplicationFactory<IAssemblyMarker>
{
    private const string TestConnectionString = "Server=localhost;Database=stufftracker_test;User=admin;Password=Password12;";
    private static readonly SemaphoreSlim _dbInitLock = new SemaphoreSlim(1, 1);
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            // Override connection string for test database
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", TestConnectionString }
            }!);
        });

        builder.ConfigureLogging(logging =>
        {
            // Reduce log verbosity during tests - suppress all EF Core logging
            logging.SetMinimumLevel(LogLevel.Error);
            logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.None);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Migrations", LogLevel.None);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.None);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.None);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<StuffTrackerDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Re-register DbContext with test connection string and minimal logging
            services.AddDbContext<StuffTrackerDbContext>(options =>
                options.UseMySql(
                    TestConnectionString,
                    ServerVersion.AutoDetect(TestConnectionString),
                    mysqlOptions => mysqlOptions.MigrationsAssembly("StuffTracker.Api")
                )
                .LogTo(_ => { }, LogLevel.None) // Suppress EF Core logging
                .EnableSensitiveDataLogging(false)
                .EnableDetailedErrors(false));
        });
    }

    /// <summary>
    /// Initialize test database by applying migrations.
    /// Call this once per test class using IClassFixture pattern, or per test if needed.
    /// Thread-safe to handle parallel test execution.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        await _dbInitLock.WaitAsync();
        try
        {
            // First, ensure the database exists by connecting without database name
            var serverConnectionString = "Server=localhost;User=admin;Password=Password12;";
            await using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync();
            
            // Create database if it doesn't exist
            try
            {
                var createDbCommand = new MySqlCommand("CREATE DATABASE IF NOT EXISTS `stufftracker_test` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;", connection);
                await createDbCommand.ExecuteNonQueryAsync();
            }
            catch (MySqlException)
            {
                // Ignore errors
            }
            
            await connection.CloseAsync();
            
            // Now work with the database
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StuffTrackerDbContext>();
            
            // Ensure test database migrations are applied first (creates tables if needed)
            await dbContext.Database.MigrateAsync();
            
            // Clear all data from tables (more efficient than dropping database)
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
                await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `Items`;");
                await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `Rooms`;");
                await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE `Locations`;");
                await dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");
            }
            catch (MySqlException)
            {
                // Tables might not exist yet (shouldn't happen after MigrateAsync, but handle gracefully)
            }
        }
        finally
        {
            _dbInitLock.Release();
        }
    }

    /// <summary>
    /// Seed the test database with test data.
    /// Call this after InitializeDatabaseAsync to populate seed data.
    /// </summary>
    public async Task SeedDatabaseAsync()
    {
        await DataSeeder.SeedAsync(Services);
    }

    /// <summary>
    /// Clean up test database (deletes all data).
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StuffTrackerDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Get a scoped DbContext for test operations.
    /// Caller is responsible for disposing the scope if needed.
    /// </summary>
    public StuffTrackerDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<StuffTrackerDbContext>();
    }

    /// <summary>
    /// Override CreateClient to disable cookie handling.
    /// This works around a .NET 9 issue on macOS where GetDomainName() fails.
    /// </summary>
    public new HttpClient CreateClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost")
        });
    }
}

