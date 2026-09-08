using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.EventService.Entities;

namespace TixTapGo.EventService.DAL.Configurations;

internal class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(@event => @event.Title).HasMaxLength(128);
        builder.Property(@event => @event.Description).HasMaxLength(1024);
        builder.Property(@event => @event.Location).HasMaxLength(256);
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Event_End_After_Start",
                $"\"{nameof(Event.End)}\" >= \"{nameof(Event.Start)}\""));
        builder.HasIndex(@event => new { @event.Status, @event.End });
        builder.HasIndex(@event => @event.VenueId);
    }
}
