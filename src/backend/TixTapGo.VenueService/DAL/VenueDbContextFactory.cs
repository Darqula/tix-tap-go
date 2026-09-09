using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TixTapGo.VenueService.DAL;

internal sealed class VenueDbContextFactory : IDesignTimeDbContextFactory<VenueDbContext>
{
    public VenueDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<VenueDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<VenueDbContext>()
            .UseNpgsql(configuration.GetConnectionString("venuesdb"))
            .Options;

        return new VenueDbContext(options, []);
    }
}
