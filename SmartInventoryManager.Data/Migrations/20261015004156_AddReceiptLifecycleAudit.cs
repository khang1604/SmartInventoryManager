using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInventoryManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptLifecycleAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ImportReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "ImportReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "ImportReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledBy",
                table: "ImportReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "ImportReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedBy",
                table: "ImportReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ExportReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "ExportReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "ExportReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledBy",
                table: "ExportReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "ExportReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedBy",
                table: "ExportReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReceiptStatusChanges",
                columns: table => new
                {
                    ReceiptStatusChangeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportReceiptId = table.Column<int>(type: "int", nullable: true),
                    ExportReceiptId = table.Column<int>(type: "int", nullable: true),
                    FromStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ToStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptStatusChanges", x => x.ReceiptStatusChangeId);
                    table.CheckConstraint("CK_ReceiptStatusChanges_Receipt", "([ImportReceiptId] IS NOT NULL AND [ExportReceiptId] IS NULL) OR ([ImportReceiptId] IS NULL AND [ExportReceiptId] IS NOT NULL)");
                    table.CheckConstraint("CK_ReceiptStatusChanges_States", "[FromStatus] IN ('DRAFT','PENDING_APPROVAL','APPROVED','REJECTED','COMPLETED','CANCELLED') AND [ToStatus] IN ('DRAFT','PENDING_APPROVAL','APPROVED','REJECTED','COMPLETED','CANCELLED') AND [FromStatus] <> [ToStatus]");
                    table.ForeignKey(
                        name: "FK_ReceiptStatusChanges_ExportReceipts_ExportReceiptId",
                        column: x => x.ExportReceiptId,
                        principalTable: "ExportReceipts",
                        principalColumn: "ExportReceiptId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptStatusChanges_ImportReceipts_ImportReceiptId",
                        column: x => x.ImportReceiptId,
                        principalTable: "ImportReceipts",
                        principalColumn: "ImportReceiptId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptStatusChanges_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceipts_ApprovedBy",
                table: "ImportReceipts",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceipts_CancelledBy",
                table: "ImportReceipts",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceipts_SubmittedBy",
                table: "ImportReceipts",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_ApprovedBy",
                table: "ExportReceipts",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_CancelledBy",
                table: "ExportReceipts",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_SubmittedBy",
                table: "ExportReceipts",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptStatusChanges_ChangedBy",
                table: "ReceiptStatusChanges",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptStatusChanges_ExportReceiptId_ChangedAt_ReceiptStatusChangeId",
                table: "ReceiptStatusChanges",
                columns: new[] { "ExportReceiptId", "ChangedAt", "ReceiptStatusChangeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptStatusChanges_ImportReceiptId_ChangedAt_ReceiptStatusChangeId",
                table: "ReceiptStatusChanges",
                columns: new[] { "ImportReceiptId", "ChangedAt", "ReceiptStatusChangeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExportReceipts_Users_ApprovedBy",
                table: "ExportReceipts",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExportReceipts_Users_CancelledBy",
                table: "ExportReceipts",
                column: "CancelledBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExportReceipts_Users_SubmittedBy",
                table: "ExportReceipts",
                column: "SubmittedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipts_Users_ApprovedBy",
                table: "ImportReceipts",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipts_Users_CancelledBy",
                table: "ImportReceipts",
                column: "CancelledBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipts_Users_SubmittedBy",
                table: "ImportReceipts",
                column: "SubmittedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportReceipts_Users_ApprovedBy",
                table: "ExportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_ExportReceipts_Users_CancelledBy",
                table: "ExportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_ExportReceipts_Users_SubmittedBy",
                table: "ExportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipts_Users_ApprovedBy",
                table: "ImportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipts_Users_CancelledBy",
                table: "ImportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipts_Users_SubmittedBy",
                table: "ImportReceipts");

            migrationBuilder.DropTable(
                name: "ReceiptStatusChanges");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceipts_ApprovedBy",
                table: "ImportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceipts_CancelledBy",
                table: "ImportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceipts_SubmittedBy",
                table: "ImportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ExportReceipts_ApprovedBy",
                table: "ExportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ExportReceipts_CancelledBy",
                table: "ExportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ExportReceipts_SubmittedBy",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "ExportReceipts");
        }
    }
}
