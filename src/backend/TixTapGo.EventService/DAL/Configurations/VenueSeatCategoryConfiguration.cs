using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TixTapGo.EventService.Entities;

namespace TixTapGo.EventService.DAL.Configurations;

internal class VenueSeatCategoryConfiguration : IEntityTypeConfiguration<VenueSeatCategory>
{
    public void Configure(EntityTypeBuilder<VenueSeatCategory> builder)
    {
        builder.Property(p => p.Title).HasMaxLength(48);
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_VenueSeatCategory_TotalCapacity_NotNegative",
                $"\"{nameof(VenueSeatCategory.TotalCapacity)}\" >= 0"));
    }
}
