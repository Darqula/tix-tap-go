using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DAL.Configurations;

internal sealed class VenueSeatMapVersionConfiguration(DbContext dbContext)
    : IEntityTypeConfiguration<VenueSeatingMapVersion>
{
    public void Configure(EntityTypeBuilder<VenueSeatingMapVersion> builder)
    {
        builder.Property(v => v.Description)
            .HasMaxLength(256);
        builder.Property(v => v.MapUrl)
            .HasMaxLength(256);

        if (dbContext.Database.IsNpgsql())
        {
            builder.Property(v => v.ValidFrom)
                .HasDefaultValueSql("now()");
        }
        else
        {
            throw new NotImplementedException(
                $"Provider-specific configuration for {dbContext.Database.ProviderName} is not implemented");
        }
    }
}
