using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using TixTapGo.Shared.Persistence.DAL;
using TixTapGo.VenueService.DAL.Configurations;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DAL;

internal sealed class VenueDbContext(
    DbContextOptions<VenueDbContext> options,
    IEnumerable<ISaveChangesInterceptor> interceptors) : SharedDbContext(options, interceptors)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<VenueSeat> VenueSeats => Set<VenueSeat>();
    public DbSet<VenueSeatingMapVersion> VenueSeatingMapVersions => Set<VenueSeatingMapVersion>();
    public DbSet<SeatCategory> SeatCategories => Set<SeatCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("btree_gist");
        }

        modelBuilder.ApplyConfiguration(new VenueConfiguration());
        modelBuilder.ApplyConfiguration(new VenueSeatingMapVersionConfiguration());
        modelBuilder.ApplyConfiguration(new VenueSeatConfiguration());
        modelBuilder.ApplyConfiguration(new SeatCategoryConfiguration());

        modelBuilder.AddOutboxMessageTables();
    }
}
