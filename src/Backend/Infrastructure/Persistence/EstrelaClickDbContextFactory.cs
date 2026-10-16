using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EstrelaClick.Backend.Infrastructure.Persistence;

public sealed class EstrelaClickDbContextFactory : IDesignTimeDbContextFactory<EstrelaClickDbContext>
{
    public EstrelaClickDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Path.GetFullPath(Path.Combine(currentDirectory, "..", "Api"));

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não foi configurada.");

        var optionsBuilder = new DbContextOptionsBuilder<EstrelaClickDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new EstrelaClickDbContext(optionsBuilder.Options);
    }
}
