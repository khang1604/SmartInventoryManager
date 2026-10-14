using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Data.Configurations
{
    public class ProductConfiguration
        : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.ProductId);

            builder.Property(p => p.ProductCode)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(p => p.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Unit)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(p => p.PurchasePrice)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(p => p.SellingPrice)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(p => p.ReorderPoint)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.MaximumStock)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("SYSDATETIME()");

            // Unique ProductCode
            builder.HasIndex(p => p.ProductCode)
                .IsUnique();

            // Business constraints
            builder.ToTable("Products", table =>
            {
                table.HasCheckConstraint(
                    "CK_Products_PurchasePrice",
                    "[PurchasePrice] >= 0");

                table.HasCheckConstraint(
                    "CK_Products_SellingPrice",
                    "[SellingPrice] >= 0");

                table.HasCheckConstraint(
                    "CK_Products_ReorderPoint",
                    "[ReorderPoint] > 0");

                table.HasCheckConstraint(
                    "CK_Products_MaximumStock",
                    "[MaximumStock] > [ReorderPoint]");
            });
        }
    }
}
