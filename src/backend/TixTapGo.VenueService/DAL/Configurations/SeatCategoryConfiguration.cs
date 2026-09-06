using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.Shared.Persistence.DAL;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DAL.Configurations;

internal sealed class SeatCategoryConfiguration : IEntityTypeConfiguration<SeatCategory>
{
    public void Configure(EntityTypeBuilder<SeatCategory> builder)
    {
        builder.Property(category => category.Title).HasMaxLength(48);
        builder.Property(category => category.Color).HasMaxLength(10);

        builder.HasIndex(category => new { category.VenueId, category.Title })
            .IsSoftDeleteUnique();
    }
}
