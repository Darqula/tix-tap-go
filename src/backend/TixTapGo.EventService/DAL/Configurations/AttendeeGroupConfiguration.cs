using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.EventService.Entities;

namespace TixTapGo.EventService.DAL.Configurations;

internal sealed class AttendeeGroupConfiguration : IEntityTypeConfiguration<AttendeeGroup>
{
    public void Configure(EntityTypeBuilder<AttendeeGroup> builder)
    {
        builder.Property(attendeeGroup => attendeeGroup.Title)
            .HasMaxLength(128);

        builder.ToTable(t =>
                t.HasCheckConstraint("CK_AttendeeGroup_Capacity_Positive", $"\"{nameof(AttendeeGroup.Capacity)}\" > 0")
            )
            .HasOne(attendeeGroup => attendeeGroup.Event)
            .WithMany(@event => @event.AttendeeGroups)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
