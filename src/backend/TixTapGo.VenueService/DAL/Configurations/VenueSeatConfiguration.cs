using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.Shared.Persistence.DAL;
using TixTapGo.VenueService.Entities;

namespace TixTapGo.VenueService.DAL.Configurations;

internal sealed class VenueSeatConfiguration : IEntityTypeConfiguration<VenueSeat>
{
    public void Configure(EntityTypeBuilder<VenueSeat> builder)
    {
        builder.Property(seat => seat.RowNumber).HasMaxLength(6);
        builder.Property(seat => seat.SeatNumber).HasMaxLength(6);
        builder.Property(seat => seat.MapPositionX).HasPrecision(6, 2);
        builder.Property(seat => seat.MapPositionY).HasPrecision(6, 2);
        builder
            .HasIndex(seat => new
            {
                seat.VenueSeatMapVersionId,
                seat.RowNumber,
                seat.SeatNumber
            })
            .IsSoftDeleteUnique();
        builder
            .HasIndex(seat => new
            {
                seat.VenueSeatMapVersionId,
                seat.MapPositionX,
                seat.MapPositionY
            })
            .IsSoftDeleteUnique();
    }
}
