using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Data.Configurations;

public class CustomerCartItemConfiguration : IEntityTypeConfiguration<CustomerCartItem>
{
    public void Configure(EntityTypeBuilder<CustomerCartItem> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");

        // Müşteri + ürün başına tek satır (upsert için).
        builder.HasIndex(x => new { x.CustomerId, x.StockItemId }).IsUnique();
    }
}
