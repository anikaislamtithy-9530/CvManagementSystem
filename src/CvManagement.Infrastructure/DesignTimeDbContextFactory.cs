using CvManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CvManagement.Infrastructure;

/// <summary>
/// Design-time factory for EF Core CLI tools.
/// Used by `dotnet ef migrations add` etc.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Look for appsettings.json in the Web project folder
        // (where the actual connection string lives)
        var basePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "CvManagement.Web");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(basePath) ? basePath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("CvManagement.Infrastructure");
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}