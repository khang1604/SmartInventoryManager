using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Data.Configurations
{
    public class ImportReceiptConfiguration
        : IEntityTypeConfiguration<ImportReceipt>
    {
        public void Configure(EntityTypeBuilder<ImportReceipt> builder)
        {
            builder.ToTable("ImportReceipts", table =>
            {
                table.HasCheckConstraint(
                    "CK_ImportReceipts_TotalAmount",
                    "[TotalAmount] >= 0");

                table.HasCheckConstraint(
                    "CK_ImportReceipts_Status",
                    "[Status] IN ('DRAFT', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED')");
            });

            builder.HasKey(i => i.ImportReceiptId);

            builder.Property(i => i.ReceiptCode)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(i => i.ReceiptDate)
                .IsRequired()
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(i => i.TotalAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(i => i.Status)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");

            builder.Property(i => i.Note)
                .HasMaxLength(500);

            builder.HasIndex(i => i.ReceiptCode)
                .IsUnique();
        }
    }

    public class ImportDetailConfiguration
        : IEntityTypeConfiguration<ImportDetail>
    {
        public void Configure(EntityTypeBuilder<ImportDetail> builder)
        {
            builder.ToTable("ImportDetails", table =>
            {
                table.HasTrigger("trg_ImportDetails_RecalculateTotal");

                table.HasCheckConstraint(
                    "CK_ImportDetails_Quantity",
                    "[Quantity] > 0");

                table.HasCheckConstraint(
                    "CK_ImportDetails_UnitPrice",
                    "[UnitPrice] >= 0");
            });

            builder.HasKey(i => i.ImportDetailId);

            builder.Property(i => i.Quantity)
                .IsRequired();

            builder.Property(i => i.UnitPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.HasIndex(i => new
            {
                i.ImportReceiptId,
                i.ProductId
            })
            .IsUnique();
        }
    }
}