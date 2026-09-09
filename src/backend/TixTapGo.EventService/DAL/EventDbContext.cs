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
    public DbSet<AttendeeGroup> AttendeeGroups => Set<AttendeeGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new AttendeeGroupConfiguration());
        modelBuilder.AddOutboxMessageTables();
    }
}
