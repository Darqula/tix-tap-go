using EventService.Entities;

using Microsoft.EntityFrameworkCore;

using Shared.DAL;

namespace EventService.DAL;

public class EventDbContext(DbContextOptions<EventDbContext> options) : SharedDbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<AttendeeGroup> AttendeeGroups => Set<AttendeeGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AttendeeGroup>()
            .ToTable(t =>
                t.HasCheckConstraint("CK_AttendeeGroup_Capacity_Positive", "\"Capacity\" > 0")
            )
            .HasOne(attendeeGroup => attendeeGroup.Event)
            .WithMany(@event => @event.AttendeeGroups)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
