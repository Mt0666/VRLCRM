using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Data.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(l => l.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(l => l.Order)
            .WithMany(o => o.Lines)
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.StockItem)
            .WithMany()
            .HasForeignKey(l => l.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
