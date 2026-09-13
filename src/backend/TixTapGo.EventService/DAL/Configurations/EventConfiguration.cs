using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.EventService.Entities;

namespace TixTapGo.EventService.DAL.Configurations;

internal class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(@event => @event.Title).HasMaxLength(128);
        builder.Property(@event => @event.Description).HasMaxLength(1024);
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_Event_End_After_Start",
                $"\"{nameof(Event.End)}\" >= \"{nameof(Event.Start)}\""));
        builder.HasIndex(@event => new
        {
            @event.Status,
            @event.End
        });
        builder.HasIndex(@event => @event.VenueId);

        // Plain scalar jsonb instead of an owned/JSON-mapped navigation: owned entity types
        // don't flip the owner's EntityState when only the owned graph changes, so our concurrency token
        // never advances for issue-only writes. Moreover, ReloadAsync can't refresh owned navigations at all.
        // Also empty collections are stored as SQL NULL rather than "[]" so a "has any issues" filter checks
        // `IS NOT NULL` instead of .Count/.Any(), which Npgsql can't do properly for a converted collection property
        builder.Property(@event => @event.ActiveIssues)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                issues => issues.Count == 0
                    ? null
                    : JsonSerializer.Serialize(issues),
                json => json == null
                    ? new List<EventIssue>()
                    : JsonSerializer.Deserialize<List<EventIssue>>(json)!,
                new ValueComparer<IReadOnlyList<EventIssue>>(
                    (a, b) => (a ?? Array.Empty<EventIssue>()).SequenceEqual(b ?? Array.Empty<EventIssue>()),
                    issues => issues.Aggregate(0, (hash, issue) => HashCode.Combine(hash, issue.GetHashCode())),
                    issues => issues.ToList()))
            .HasColumnType("jsonb")
            .IsRequired(false);
    }
}
