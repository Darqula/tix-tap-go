using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DAL.Configurations;

internal sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.Property(venue => venue.Title)
            .HasMaxLength(128);
        builder.Property(venue => venue.Address)
            .HasMaxLength(256);
        builder.Property(venue => venue.Description)
            .HasMaxLength(1024);

        builder.HasMany(venue => venue.SeatingMapVersions)
            .WithOne(version => version.Venue)
            .HasForeignKey(version => version.VenueId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(venue => venue.SeatCategories)
            .WithOne(category => category.Venue)
            .HasForeignKey(category => category.VenueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
