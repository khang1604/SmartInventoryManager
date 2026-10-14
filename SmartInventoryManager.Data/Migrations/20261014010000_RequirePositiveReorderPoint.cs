using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Data.Migrations;

[DbContext(typeof(InventoryDbContext))]
[Migration("20261014010000_RequirePositiveReorderPoint")]
public sealed class RequirePositiveReorderPoint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Stop before changing the constraint if existing data needs an owner-approved correction.
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Products] WHERE [ReorderPoint] <= 0) THROW 51000, 'Products with nonpositive ReorderPoint must be reviewed before applying this migration.', 1;");
        migrationBuilder.DropCheckConstraint("CK_Products_ReorderPoint", "Products");
        migrationBuilder.AddCheckConstraint("CK_Products_ReorderPoint", "Products", "[ReorderPoint] > 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_Products_ReorderPoint", "Products");
        migrationBuilder.AddCheckConstraint("CK_Products_ReorderPoint", "Products", "[ReorderPoint] >= 0");
    }
}
