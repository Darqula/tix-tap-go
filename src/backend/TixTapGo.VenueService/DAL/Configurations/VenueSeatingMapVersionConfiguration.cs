using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DAL.Configurations;

internal sealed class VenueSeatingMapVersionConfiguration
    : IEntityTypeConfiguration<VenueSeatingMapVersion>
{
    public void Configure(EntityTypeBuilder<VenueSeatingMapVersion> builder)
    {
        builder.Property(v => v.Description)
            .HasMaxLength(256);
        builder.Property(v => v.MapUrl)
            .HasMaxLength(256);
        builder.Property(v => v.IsDraft)
            .HasDefaultValue(true);
    }
}
