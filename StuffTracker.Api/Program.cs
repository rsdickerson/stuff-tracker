using Microsoft.EntityFrameworkCore;
using StuffTracker.Api.Data;
using StuffTracker.Api.GraphQL;
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
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions => mysqlOptions.MigrationsAssembly("StuffTracker.Api")
    ));

// Configure Hot Chocolate GraphQL server
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<StuffTracker.Api.GraphQL.Types.Location>()
    .AddType<StuffTracker.Api.GraphQL.Types.Room>()
    .AddType<StuffTracker.Api.GraphQL.Types.Item>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

// Apply migrations and seed database (only in development environment)
if (app.Environment.IsDevelopment())
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
