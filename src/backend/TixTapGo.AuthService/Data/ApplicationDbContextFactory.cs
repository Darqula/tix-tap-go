using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TixTapGo.AuthService.Data;

internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Default value using by OpenIddict for key stores internally.
    /// Must be provided to keep the DB model aligned with Program.cs
    /// </summary>
    private const int IdentityModelKeyMaxLength = 128;
    
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Mirrors the model-relevant registrations from Program.cs (Identity + OpenIddict)
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("authdb"));
            options.UseOpenIddict();
        });
        
        services.AddOpenIddict()
            .AddCore(options =>
                options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>());

        services.AddIdentityCore<IdentityUser>(options =>
            {
                options.Stores.MaxLengthForKeys = IdentityModelKeyMaxLength;
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ApplicationDbContext>();
    }
}
