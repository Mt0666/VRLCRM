using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Data.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");

        builder.Property(s => s.StockCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.StockCode)
            .IsUnique();

        builder.Property(s => s.Barcode)
            .HasMaxLength(100);

        builder.HasIndex(s => s.Barcode);

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(500);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Price)
            .HasPrecision(18, 2);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(s => s.Category)
            .WithMany(c => c.StockItems)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
