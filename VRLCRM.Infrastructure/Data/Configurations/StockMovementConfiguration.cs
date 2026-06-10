using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.Property(m => m.Notes)
            .HasMaxLength(500);

        builder.Property(m => m.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(m => m.StockItem)
            .WithMany()
            .HasForeignKey(m => m.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ReferenceType, m.ReferenceId });
    }
}
