using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInventoryManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptAuditAndMovementMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "TransferReferenceSequence");

            migrationBuilder.AddColumn<int>(
                name: "AfterQuantity",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BeforeQuantity",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignedDifference",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "ImportReceipts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ImportReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedBy",
                table: "ImportReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "ImportReceipts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "ExportReceipts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ExportReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedBy",
                table: "ExportReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "ExportReceipts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReceiptRejections",
                columns: table => new
                {
                    ReceiptRejectionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportReceiptId = table.Column<int>(type: "int", nullable: true),
                    ExportReceiptId = table.Column<int>(type: "int", nullable: true),
                    RejectedBy = table.Column<int>(type: "int", nullable: false),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptRejections", x => x.ReceiptRejectionId);
                    table.CheckConstraint("CK_ReceiptRejections_Receipt", "([ImportReceiptId] IS NOT NULL AND [ExportReceiptId] IS NULL) OR ([ImportReceiptId] IS NULL AND [ExportReceiptId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ReceiptRejections_ExportReceipts_ExportReceiptId",
                        column: x => x.ExportReceiptId,
                        principalTable: "ExportReceipts",
                        principalColumn: "ExportReceiptId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptRejections_ImportReceipts_ImportReceiptId",
                        column: x => x.ImportReceiptId,
                        principalTable: "ImportReceipts",
                        principalColumn: "ImportReceiptId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptRejections_Users_RejectedBy",
                        column: x => x.RejectedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_InventoryTransactions_TransferReferenceType",
                table: "InventoryTransactions",
                columns: new[] { "ReferenceId", "TransactionType" },
                unique: true,
                filter: "[ReferenceId] IS NOT NULL AND [TransactionType] IN ('TRANSFER_IN', 'TRANSFER_OUT')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryTransactions_QuantityAudit",
                table: "InventoryTransactions",
                sql: "([BeforeQuantity] IS NULL AND [AfterQuantity] IS NULL AND [SignedDifference] IS NULL) OR ([BeforeQuantity] IS NOT NULL AND [AfterQuantity] IS NOT NULL AND [SignedDifference] IS NOT NULL AND [BeforeQuantity] >= 0 AND [AfterQuantity] >= 0 AND CONVERT(bigint, [AfterQuantity]) - [BeforeQuantity] = [SignedDifference])");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceipts_CompletedBy",
                table: "ImportReceipts",
                column: "CompletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_CompletedBy",
                table: "ExportReceipts",
                column: "CompletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptRejections_ExportReceiptId_RejectedAt",
                table: "ReceiptRejections",
                columns: new[] { "ExportReceiptId", "RejectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptRejections_ImportReceiptId_RejectedAt",
                table: "ReceiptRejections",
                columns: new[] { "ImportReceiptId", "RejectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptRejections_RejectedBy",
                table: "ReceiptRejections",
                column: "RejectedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ExportReceipts_Users_CompletedBy",
                table: "ExportReceipts",
                column: "CompletedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipts_Users_CompletedBy",
                table: "ImportReceipts",
                column: "CompletedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportReceipts_Users_CompletedBy",
                table: "ExportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipts_Users_CompletedBy",
                table: "ImportReceipts");

            migrationBuilder.DropTable(
                name: "ReceiptRejections");

            migrationBuilder.DropIndex(
                name: "UX_InventoryTransactions_TransferReferenceType",
                table: "InventoryTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryTransactions_QuantityAudit",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceipts_CompletedBy",
                table: "ImportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_ExportReceipts_CompletedBy",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "AfterQuantity",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "BeforeQuantity",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "SignedDifference",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "ImportReceipts");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "ExportReceipts");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "ExportReceipts");

            migrationBuilder.DropSequence(
                name: "TransferReferenceSequence");
        }
    }
}
