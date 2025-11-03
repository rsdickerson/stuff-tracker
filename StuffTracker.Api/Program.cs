using Microsoft.EntityFrameworkCore;
using StuffTracker.Api.Data;
using StuffTracker.Api.GraphQL;
using StuffTracker.Api.GraphQL.Types;
using StuffTracker.Api.GraphQL.Sorting;
using StuffTracker.Domain.Data;
using HotChocolate.Data;
using HotChocolate.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure MySQL connection and DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<StuffTrackerDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 21)), // MySQL 8.0.21+
        mysqlOptions => mysqlOptions.MigrationsAssembly("StuffTracker.Api")
    ));

// Configure Hot Chocolate GraphQL server
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<LocationType>()
    .AddType<RoomType>()
    .AddType<ItemType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddType<LocationSortType>()
    .AddType<ItemSortType>()
    .AddDbContextCursorPagingProvider() // Enable EF Core keyset pagination
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .ModifyPagingOptions(opt =>
    {
        opt.DefaultPageSize = 50;
        opt.MaxPageSize = 1000;
        opt.IncludeTotalCount = true;
        opt.RequirePagingBoundaries = false;
    });




var app = builder.Build();

// Apply migrations and seed database (only in development environment, not during tests)
if (app.Environment.IsDevelopment() && app.Environment.EnvironmentName != "Test")
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<StuffTrackerDbContext>();
    
    // Apply pending migrations
    await dbContext.Database.MigrateAsync();
    
    // Seed database
    await DataSeeder.SeedAsync(app.Services);
}

// Map GraphQL endpoint with IDE enabled in development only
app.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    Tool =
    {
        Enable = app.Environment.IsDevelopment()
    }
});

app.MapGet("/", () => "Hello World!");

app.Run();

// Make Program class accessible for testing with WebApplicationFactory
public partial class Program { }
