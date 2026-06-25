using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Data.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(l => l.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(l => l.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.StockItem)
            .WithMany()
            .HasForeignKey(l => l.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
