using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Data.Configurations
{
    public class InventoryConfiguration
        : IEntityTypeConfiguration<Inventory>
    {
        // Cấu hình ánh xạ, quan hệ và ràng buộc dữ liệu của tồn kho.
        public void Configure(EntityTypeBuilder<Inventory> builder)
        {
            builder.ToTable("Inventory");

            builder.HasKey(i => i.InventoryId);

            builder.Property(i => i.ProductId)
                .IsRequired();

            builder.Property(i => i.WarehouseId)
                .IsRequired();

            builder.Property(i => i.Quantity)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(i => i.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("SYSDATETIME()");

            // Một Product chỉ có một Inventory record
            // tại một Warehouse.
            builder.HasIndex(i => new
            {
                i.ProductId,
                i.WarehouseId
            })
            .IsUnique();

            builder.ToTable("Inventory", table =>
            {
                table.HasCheckConstraint(
                    "CK_Inventory_Quantity",
                    "[Quantity] >= 0");
            });
        }
    }
}