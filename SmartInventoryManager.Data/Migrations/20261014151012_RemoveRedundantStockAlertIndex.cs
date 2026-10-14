using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInventoryManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantStockAlertIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_StockAlerts_ProductId_WarehouseId_AlertType] ON [dbo].[StockAlerts]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StockAlerts') AND name = N'IX_StockAlerts_ProductId_WarehouseId_AlertType') CREATE UNIQUE INDEX [IX_StockAlerts_ProductId_WarehouseId_AlertType] ON [dbo].[StockAlerts] ([ProductId], [WarehouseId], [AlertType]) WHERE [IsResolved] = 0");
        }
    }
}
