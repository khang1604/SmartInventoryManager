using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Data.Context
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options)
        {
        }

        // =========================
        // DbSets
        // =========================

        public DbSet<Role> Roles => Set<Role>();

        public DbSet<User> Users => Set<User>();

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Supplier> Suppliers => Set<Supplier>();

        public DbSet<Warehouse> Warehouses => Set<Warehouse>();

        public DbSet<Location> Locations => Set<Location>();

        public DbSet<Inventory> Inventories => Set<Inventory>();

        public DbSet<ImportReceipt> ImportReceipts => Set<ImportReceipt>();

        public DbSet<ImportDetail> ImportDetails => Set<ImportDetail>();

        public DbSet<ExportReceipt> ExportReceipts => Set<ExportReceipt>();

        public DbSet<ExportDetail> ExportDetails => Set<ExportDetail>();

        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

        public DbSet<StockAlert> StockAlerts => Set<StockAlert>();

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<SystemLog> SystemLogs => Set<SystemLog>();


        public DbSet<UserWarehouse> UserWarehouses => Set<UserWarehouse>();



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // Apply the 3 locked configurations
            // ==========================================

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(InventoryDbContext).Assembly);


            // ==========================================
            // ROLE
            // Role 1 : N User
            // ==========================================

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");

                entity.HasKey(r => r.RoleId);

                entity.Property(r => r.RoleName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(r => r.Description)
                    .HasMaxLength(255);

                entity.HasIndex(r => r.RoleName)
                    .IsUnique();
            });


            // ==========================================
            // USER
            // ==========================================

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(u => u.UserId);

                entity.Property(u => u.Username)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(u => u.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Email)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(u => u.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(u => u.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasIndex(u => u.Username)
                    .IsUnique();

                // Role 1:N User
                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // CATEGORY
            // Category 1:N Product
            // ==========================================

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");

                entity.HasKey(c => c.CategoryId);

                entity.Property(c => c.CategoryName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.Description)
                    .HasMaxLength(255);

                entity.Property(c => c.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasIndex(c => c.CategoryName)
                    .IsUnique();

                entity.HasMany(c => c.Products)
                    .WithOne(p => p.Category)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // SUPPLIER
            // Supplier 1:N ImportReceipt
            // ==========================================

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");

                entity.HasKey(s => s.SupplierId);

                entity.Property(s => s.SupplierCode)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(s => s.SupplierName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(s => s.Phone)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(s => s.Email)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(s => s.Address)
                    .HasMaxLength(255);

                entity.Property(s => s.TaxCode)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(s => s.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(s => s.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasIndex(s => s.SupplierCode)
                    .IsUnique();
            });


            // ==========================================
            // WAREHOUSE
            // ==========================================

            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.ToTable("Warehouses");

                entity.HasKey(w => w.WarehouseId);

                entity.Property(w => w.WarehouseCode)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(w => w.WarehouseName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(w => w.Location)
                    .HasMaxLength(255);

                entity.Property(w => w.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(w => w.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasIndex(w => w.WarehouseCode)
                    .IsUnique();

                // User 1:N Warehouse
                // ManagerId nullable
                entity.HasOne(w => w.Manager)
                    .WithMany(u => u.ManagedWarehouses)
                    .HasForeignKey(w => w.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // LOCATION
            // Warehouse 1:N Location
            // ==========================================

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");

                entity.HasKey(l => l.LocationId);

                entity.Property(l => l.LocationCode)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(l => l.LocationName)
                    .HasMaxLength(100);

                entity.Property(l => l.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasIndex(l => new
                {
                    l.WarehouseId,
                    l.LocationCode
                })
                .IsUnique();

                entity.HasOne(l => l.Warehouse)
                    .WithMany(w => w.Locations)
                    .HasForeignKey(l => l.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // INVENTORY
            // Product N:N Warehouse
            // resolved by Inventory
            // ==========================================

            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasOne(i => i.Product)
                    .WithMany(p => p.Inventories)
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Warehouse)
                    .WithMany(w => w.Inventories)
                    .HasForeignKey(i => i.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // IMPORT RECEIPT
            // ==========================================

            modelBuilder.Entity<ImportReceipt>(entity =>
            {
                entity.ToTable("ImportReceipts", table =>
                {
                    table.HasCheckConstraint(
                        "CK_ImportReceipts_TotalAmount",
                        "[TotalAmount] >= 0");

                    table.HasCheckConstraint(
                        "CK_ImportReceipts_Status",
                        "[Status] IN ('DRAFT', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED')");
                });

                entity.HasKey(i => i.ImportReceiptId);

                entity.Property(i => i.ReceiptCode)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(i => i.ReceiptDate)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(i => i.TotalAmount)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0);

                entity.Property(i => i.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValue("DRAFT");

                entity.Property(i => i.Note)
                    .HasMaxLength(500);

                entity.HasIndex(i => i.ReceiptCode)
                    .IsUnique();

                entity.HasOne(i => i.Supplier)
                    .WithMany(s => s.ImportReceipts)
                    .HasForeignKey(i => i.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Warehouse)
                    .WithMany(w => w.ImportReceipts)
                    .HasForeignKey(i => i.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Creator)
                    .WithMany(u => u.CreatedImportReceipts)
                    .HasForeignKey(i => i.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(i => i.Details)
                    .WithOne(d => d.ImportReceipt)
                    .HasForeignKey(d => d.ImportReceiptId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // IMPORT DETAIL
            // ==========================================

            modelBuilder.Entity<ImportDetail>(entity =>
            {
                // Product 1:N ImportDetail
                entity.HasOne(d => d.Product)
                    .WithMany(p => p.ImportDetails)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // EXPORT RECEIPT
            // ==========================================

            modelBuilder.Entity<ExportReceipt>(entity =>
            {
                entity.ToTable("ExportReceipts", table =>
                {
                    table.HasCheckConstraint(
                        "CK_ExportReceipts_TotalAmount",
                        "[TotalAmount] >= 0");

                    table.HasCheckConstraint(
                        "CK_ExportReceipts_Status",
                        "[Status] IN ('DRAFT', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED')");
                });

                entity.HasKey(e => e.ExportReceiptId);

                entity.Property(e => e.ReceiptCode)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ReceiptDate)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(e => e.TotalAmount)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValue("DRAFT");

                entity.Property(e => e.Note)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.ReceiptCode)
                    .IsUnique();

                entity.HasOne(e => e.Warehouse)
                    .WithMany(w => w.ExportReceipts)
                    .HasForeignKey(e => e.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedExportReceipts)
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Details)
                    .WithOne(d => d.ExportReceipt)
                    .HasForeignKey(d => d.ExportReceiptId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // EXPORT DETAIL
            // ==========================================

            modelBuilder.Entity<ExportDetail>(entity =>
            {
                entity.ToTable("ExportDetails");

                entity.HasKey(e => e.ExportDetailId);

                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.UnitPrice)
                    .IsRequired()
                    .HasPrecision(18, 2);

                entity.HasIndex(e => new
                {
                    e.ExportReceiptId,
                    e.ProductId
                })
                .IsUnique();

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.ExportDetails)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable("ExportDetails", table =>
                {
                    table.HasCheckConstraint(
                        "CK_ExportDetails_Quantity",
                        "[Quantity] > 0");

                    table.HasCheckConstraint(
                        "CK_ExportDetails_UnitPrice",
                        "[UnitPrice] >= 0");
                });
            });


            // ==========================================
            // INVENTORY TRANSACTION
            // ==========================================

            modelBuilder.Entity<InventoryTransaction>(entity =>
            {
                entity.ToTable("InventoryTransactions");

                entity.HasKey(t => t.TransactionId);

                entity.Property(t => t.TransactionType)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(t => t.ReferenceType)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(t => t.Note)
                    .HasMaxLength(500);

                entity.Property(t => t.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(t => t.Product)
                    .WithMany(p => p.InventoryTransactions)
                    .HasForeignKey(t => t.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Warehouse)
                    .WithMany(w => w.InventoryTransactions)
                    .HasForeignKey(t => t.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Creator)
                    .WithMany(u => u.InventoryTransactions)
                    .HasForeignKey(t => t.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable("InventoryTransactions", table =>
                {
                    table.HasCheckConstraint(
                        "CK_InventoryTransactions_Quantity",
                        "[Quantity] > 0");

                    table.HasCheckConstraint(
                        "CK_InventoryTransactions_TransactionType",
                        "[TransactionType] IN " +
                        "('IMPORT', 'EXPORT', 'ADJUSTMENT', " +
                        "'TRANSFER_IN', 'TRANSFER_OUT')");

                    table.HasCheckConstraint(
                        "CK_InventoryTransactions_ReferenceType",
                        "[ReferenceType] IS NULL OR " +
                        "[ReferenceType] IN " +
                        "('IMPORT_RECEIPT', 'EXPORT_RECEIPT', 'MANUAL')");
                });
            });


            // ==========================================
            // STOCK ALERT
            // ==========================================

            modelBuilder.Entity<StockAlert>(entity =>
            {
                entity.ToTable("StockAlerts");

                entity.HasKey(a => a.AlertId);

                entity.HasIndex(a => new
                {
                    a.ProductId,
                    a.WarehouseId,
                    a.AlertType
                })
                .IsUnique()
                .HasFilter("[IsResolved] = 0");

                entity.Property(a => a.AlertType)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(a => a.CurrentQuantity)
                    .IsRequired();

                entity.Property(a => a.IsResolved)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(a => a.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(a => a.Product)
                    .WithMany(p => p.StockAlerts)
                    .HasForeignKey(a => a.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Warehouse)
                    .WithMany(w => w.StockAlerts)
                    .HasForeignKey(a => a.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ResolvedBy nullable
                entity.HasOne(a => a.Resolver)
                    .WithMany(u => u.ResolvedStockAlerts)
                    .HasForeignKey(a => a.ResolvedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable("StockAlerts", table =>
                {
                    table.HasCheckConstraint(
                        "CK_StockAlerts_AlertType",
                        "[AlertType] IN " +
                        "('LOW_STOCK', 'OUT_OF_STOCK', 'OVERSTOCK')");

                    table.HasCheckConstraint(
                        "CK_StockAlerts_CurrentQuantity",
                        "[CurrentQuantity] >= 0");

                    table.HasCheckConstraint(
                        "CK_StockAlerts_Resolution",
                        "([IsResolved] = 0 AND [ResolvedAt] IS NULL) " +
                        "OR " +
                        "([IsResolved] = 1 AND [ResolvedAt] IS NOT NULL)");
                });
            });


            // ==========================================
            // NOTIFICATION
            // ==========================================

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");

                entity.HasKey(n => n.NotificationId);

                entity.Property(n => n.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(n => n.NotificationType)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(n => n.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(n => n.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ==========================================
            // SYSTEM LOG
            // ==========================================

            modelBuilder.Entity<SystemLog>(entity =>
            {
                entity.ToTable("SystemLogs");

                entity.HasKey(l => l.LogId);

                entity.Property(l => l.Action)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(l => l.EntityName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(l => l.Description)
                    .HasMaxLength(1000);

                entity.Property(l => l.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                // User 0:N SystemLog
                entity.HasOne(l => l.User)
                    .WithMany(u => u.SystemLogs)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // USER WAREHOUSE
            // ==========================================
            modelBuilder.Entity<UserWarehouse>(entity =>
            {
                entity.ToTable("UserWarehouses");

                entity.HasKey(x => new { x.UserId, x.WarehouseId });

                entity.HasOne(x => x.User)
                    .WithMany(x => x.UserWarehouses)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Warehouse)
                    .WithMany(x => x.UserWarehouses)
                    .HasForeignKey(x => x.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
