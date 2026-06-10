using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);
    }
}
