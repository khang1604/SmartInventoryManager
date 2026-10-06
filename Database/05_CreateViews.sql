-- SmartInventoryManager database script skeleton: CreateViews
-- Status: not implemented. Do not execute or use this file to create a database.
USE SmartInventoryManagerDB;
GO

/* =========================================================
   05_CreateViews.sql

   Purpose:
   - Reusable read-only queries
   - Dashboard
   - Inventory reports
   - Stock alerts
   - Import / Export analytics

   IMPORTANT:
   Views do NOT modify data.
   Inventory status is calculated from:
       Inventory.Quantity
       Product.ReorderPoint
       Product.MaximumStock
   ========================================================= */


/* =========================================================
   VIEW 1
   INVENTORY STATUS
   ========================================================= */

CREATE OR ALTER VIEW dbo.vw_InventoryStatus
AS
SELECT
    i.InventoryId,

    p.ProductId,
    p.ProductCode,
    p.ProductName,

    c.CategoryId,
    c.CategoryName,

    w.WarehouseId,
    w.WarehouseCode,
    w.WarehouseName,

    i.Quantity,

    p.ReorderPoint,
    p.MaximumStock,

    p.PurchasePrice,
    p.SellingPrice,

    CASE
        WHEN i.Quantity = 0
            THEN 'OUT_OF_STOCK'

        WHEN i.Quantity > 0
             AND i.Quantity <= p.ReorderPoint
            THEN 'LOW_STOCK'

        WHEN i.Quantity > p.MaximumStock
            THEN 'OVERSTOCK'

        ELSE 'NORMAL'
    END AS StockStatus,

    CAST(
        i.Quantity * p.PurchasePrice
        AS DECIMAL(18,2)
    ) AS InventoryCostValue,

    CAST(
        i.Quantity * p.SellingPrice
        AS DECIMAL(18,2)
    ) AS PotentialSellingValue,

    i.UpdatedAt

FROM dbo.Inventory AS i

INNER JOIN dbo.Products AS p
    ON p.ProductId = i.ProductId

INNER JOIN dbo.Categories AS c
    ON c.CategoryId = p.CategoryId

INNER JOIN dbo.Warehouses AS w
    ON w.WarehouseId = i.WarehouseId;
GO


/* =========================================================
   VIEW 2
   INVENTORY VALUE BY WAREHOUSE
   ========================================================= */

CREATE OR ALTER VIEW dbo.vw_InventoryValue
AS
SELECT
    WarehouseId,
    WarehouseCode,
    WarehouseName,

    SUM(Quantity) AS TotalQuantity,

    CAST(
        SUM(InventoryCostValue)
        AS DECIMAL(18,2)
    ) AS InventoryCostValue,

    CAST(
        SUM(PotentialSellingValue)
        AS DECIMAL(18,2)
    ) AS PotentialSellingValue

FROM dbo.vw_InventoryStatus

GROUP BY
    WarehouseId,
    WarehouseCode,
    WarehouseName;
GO


/* =========================================================
   VIEW 3
   LOW / OUT OF STOCK PRODUCTS
   ========================================================= */

CREATE OR ALTER VIEW dbo.vw_LowStockProducts
AS
SELECT
    InventoryId,

    ProductId,
    ProductCode,
    ProductName,

    CategoryId,
    CategoryName,

    WarehouseId,
    WarehouseCode,
    WarehouseName,

    Quantity,
    ReorderPoint,
    MaximumStock,

    StockStatus,

    UpdatedAt

FROM dbo.vw_InventoryStatus

WHERE StockStatus IN
(
    'LOW_STOCK',
    'OUT_OF_STOCK'
);
GO


/* =========================================================
   VIEW 4
   MONTHLY IMPORT / EXPORT
   ========================================================= */

CREATE OR ALTER VIEW dbo.vw_MonthlyImportExport
AS

WITH ImportData AS
(
    SELECT
        YEAR(ir.ReceiptDate) AS [Year],
        MONTH(ir.ReceiptDate) AS [Month],

        SUM(id.Quantity) AS ImportQuantity,

        CAST(
            SUM(id.Quantity * id.UnitPrice)
            AS DECIMAL(18,2)
        ) AS ImportValue

    FROM dbo.ImportReceipts AS ir

    INNER JOIN dbo.ImportDetails AS id
        ON id.ImportReceiptId = ir.ImportReceiptId

    WHERE ir.Status = 'COMPLETED'

    GROUP BY
        YEAR(ir.ReceiptDate),
        MONTH(ir.ReceiptDate)
),

ExportData AS
(
    SELECT
        YEAR(er.ReceiptDate) AS [Year],
        MONTH(er.ReceiptDate) AS [Month],

        SUM(ed.Quantity) AS ExportQuantity,

        CAST(
            SUM(ed.Quantity * ed.UnitPrice)
            AS DECIMAL(18,2)
        ) AS ExportValue

    FROM dbo.ExportReceipts AS er

    INNER JOIN dbo.ExportDetails AS ed
        ON ed.ExportReceiptId = er.ExportReceiptId

    WHERE er.Status = 'COMPLETED'

    GROUP BY
        YEAR(er.ReceiptDate),
        MONTH(er.ReceiptDate)
)

SELECT
    COALESCE(i.[Year], e.[Year]) AS [Year],

    COALESCE(i.[Month], e.[Month]) AS [Month],

    ISNULL(i.ImportQuantity, 0) AS ImportQuantity,

    ISNULL(i.ImportValue, 0) AS ImportValue,

    ISNULL(e.ExportQuantity, 0) AS ExportQuantity,

    ISNULL(e.ExportValue, 0) AS ExportValue

FROM ImportData AS i

FULL OUTER JOIN ExportData AS e
    ON i.[Year] = e.[Year]
   AND i.[Month] = e.[Month];
GO


PRINT '05_CreateViews.sql completed successfully.';
GO