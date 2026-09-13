using TixTapGo.EventService.DAL.Configurations;
using TixTapGo.EventService.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using TixTapGo.Shared.Persistence.DAL;

namespace TixTapGo.EventService.DAL;

internal sealed class EventDbContext(
    DbContextOptions<EventDbContext> options,
    IEnumerable<ISaveChangesInterceptor> interceptors) : SharedDbContext(options, interceptors)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<VenueSeatCategory> VenueSeatCategories => Set<VenueSeatCategory>();
    public DbSet<SeatCategoryPrice> SeatCategoryPrices => Set<SeatCategoryPrice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new VenueSeatCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SeatCategoryPriceConfiguration());
        modelBuilder.AddOutboxMessageTables();
    }
}
