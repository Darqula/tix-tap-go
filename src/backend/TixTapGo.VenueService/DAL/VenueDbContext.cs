using Microsoft.EntityFrameworkCore;

using TixTapGo.Shared.DAL;
using TixTapGo.VenueService.DAL.Configurations;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DAL;

internal sealed class VenueDbContext(DbContextOptions<VenueDbContext> options) : SharedDbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<VenueSeatingMapVersion> VenueSeatMapVersions => Set<VenueSeatingMapVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("btree_gist");
        }

        modelBuilder.ApplyConfiguration(new VenueConfiguration());
        modelBuilder.ApplyConfiguration(new VenueSeatMapVersionConfiguration(this));
    }
}
