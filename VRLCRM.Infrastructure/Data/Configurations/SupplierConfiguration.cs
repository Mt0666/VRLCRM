using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.Property(s => s.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.ContactName)
            .HasMaxLength(100);

        builder.Property(s => s.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.TaxNumber)
            .HasMaxLength(20);

        builder.Property(s => s.Notes)
            .HasMaxLength(2000);

        builder.Property(s => s.Balance)
            .HasPrecision(18, 2);

        builder.Property(s => s.CreditLimit)
            .HasPrecision(18, 2);

        builder.Property(s => s.City)
            .HasMaxLength(100);

        builder.Property(s => s.District)
            .HasMaxLength(100);

        builder.Property(s => s.AddressLine)
            .HasMaxLength(500);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);
    }
}
