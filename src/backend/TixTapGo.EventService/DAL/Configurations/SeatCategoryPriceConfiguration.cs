using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.EventService.Entities;

namespace TixTapGo.EventService.DAL.Configurations;

internal class SeatCategoryPriceConfiguration : IEntityTypeConfiguration<SeatCategoryPrice>
{
    public void Configure(EntityTypeBuilder<SeatCategoryPrice> builder)
    {
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_EventSeatCategoryPrice_BasePrice_NotNegative",
                $"\"{nameof(SeatCategoryPrice.BasePrice)}\" >= 0")
        );
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_EventSeatCategoryPrice_Capacity_NotNegative",
                $"\"{nameof(SeatCategoryPrice.Capacity)}\" >= 0")
        );
    }
}
