using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DAL.Configurations;

internal sealed class SeatCategoryConfiguration : IEntityTypeConfiguration<SeatCategory>
{
    public void Configure(EntityTypeBuilder<SeatCategory> builder)
    {
        builder.Property(category => category.Title).HasMaxLength(48);
        builder.Property(category => category.Color).HasMaxLength(10);
    }
}
