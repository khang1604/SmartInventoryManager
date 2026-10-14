using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInventoryManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserWarehouseActiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserWarehouses",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserWarehouses");
        }
    }
}
