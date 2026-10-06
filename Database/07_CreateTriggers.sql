-- SmartInventoryManager database script skeleton: CreateTriggers
-- Status: not implemented. Do not execute or use this file to create a database.
-- Inventory quantity changes must be coordinated by Business Services; do not implement auto-increment/decrement triggers.
USE SmartInventoryManagerDB;
GO

/* =========================================================
   07_CreateTriggers.sql

   Trigger policy:

   ALLOWED:
   - Database-level consistency
   - Derived receipt total

   NOT ALLOWED:
   - Update Inventory.Quantity
   - Create InventoryTransactions
   - Create StockAlerts
   - Execute Business workflow

   InventoryService remains the single authoritative
   stock-update path.
   ========================================================= */


/* =========================================================
   1. IMPORT DETAILS
   RECALCULATE IMPORT RECEIPT TOTAL
   ========================================================= */

CREATE OR ALTER TRIGGER dbo.trg_ImportDetails_RecalculateTotal
ON dbo.ImportDetails
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    /*
        Collect every affected ImportReceiptId.

        INSERT:
            appears in inserted

        DELETE:
            appears in deleted

        UPDATE:
            old and new receipt IDs are both handled
    */

    ;WITH AffectedReceipts AS
    (
        SELECT ImportReceiptId
        FROM inserted

        UNION

        SELECT ImportReceiptId
        FROM deleted
    )

    UPDATE ir

    SET ir.TotalAmount =
    ISNULL
    (
        (
            SELECT
                SUM
                (
                    CAST(id.Quantity AS DECIMAL(18,2))
                    * id.UnitPrice
                )

            FROM dbo.ImportDetails AS id

            WHERE
                id.ImportReceiptId = ir.ImportReceiptId
        ),
        0
    )

    FROM dbo.ImportReceipts AS ir

    INNER JOIN AffectedReceipts AS ar
        ON ar.ImportReceiptId = ir.ImportReceiptId;
END;
GO


/* =========================================================
   2. EXPORT DETAILS
   RECALCULATE EXPORT RECEIPT TOTAL
   ========================================================= */

CREATE OR ALTER TRIGGER dbo.trg_ExportDetails_RecalculateTotal
ON dbo.ExportDetails
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH AffectedReceipts AS
    (
        SELECT ExportReceiptId
        FROM inserted

        UNION

        SELECT ExportReceiptId
        FROM deleted
    )

    UPDATE er

    SET er.TotalAmount =
    ISNULL
    (
        (
            SELECT
                SUM
                (
                    CAST(ed.Quantity AS DECIMAL(18,2))
                    * ed.UnitPrice
                )

            FROM dbo.ExportDetails AS ed

            WHERE
                ed.ExportReceiptId = er.ExportReceiptId
        ),
        0
    )

    FROM dbo.ExportReceipts AS er

    INNER JOIN AffectedReceipts AS ar
        ON ar.ExportReceiptId = er.ExportReceiptId;
END;
GO


PRINT '07_CreateTriggers.sql completed successfully.';
GO