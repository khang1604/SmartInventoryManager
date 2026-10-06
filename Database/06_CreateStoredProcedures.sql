-- SmartInventoryManager database script skeleton: CreateStoredProcedures
-- Status: not implemented. Do not execute or use this file to create a database.
USE SmartInventoryManagerDB;
GO

/* =========================================================
   06_CreateStoredProcedures.sql

   Purpose:
   - Dashboard
   - Reports
   - Analytics
   - ADO.NET data provider

   NOTE:
   These procedures are READ-ONLY.
   They do NOT modify Inventory.Quantity.
   ========================================================= */


/* =========================================================
   1. INVENTORY SUMMARY
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_GetInventorySummary
    @WarehouseId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

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

        PurchasePrice,
        SellingPrice,

        StockStatus,

        InventoryCostValue,
        PotentialSellingValue,

        UpdatedAt

    FROM dbo.vw_InventoryStatus

    WHERE
        @WarehouseId IS NULL
        OR WarehouseId = @WarehouseId

    ORDER BY
        CASE StockStatus
            WHEN 'OUT_OF_STOCK' THEN 1
            WHEN 'LOW_STOCK' THEN 2
            WHEN 'OVERSTOCK' THEN 3
            WHEN 'NORMAL' THEN 4
            ELSE 5
        END,

        ProductName;
END;
GO


/* =========================================================
   2. LOW STOCK PRODUCTS
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_GetLowStockProducts
    @WarehouseId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        InventoryId,

        ProductId,
        ProductCode,
        ProductName,

        CategoryName,

        WarehouseId,
        WarehouseCode,
        WarehouseName,

        Quantity,
        ReorderPoint,
        MaximumStock,

        StockStatus,

        UpdatedAt

    FROM dbo.vw_LowStockProducts

    WHERE
        @WarehouseId IS NULL
        OR WarehouseId = @WarehouseId

    ORDER BY
        CASE StockStatus
            WHEN 'OUT_OF_STOCK' THEN 1
            WHEN 'LOW_STOCK' THEN 2
            ELSE 3
        END,

        Quantity ASC,

        ProductName;
END;
GO


/* =========================================================
   3. TOP PRODUCTS
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_GetTopProducts
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @WarehouseId INT = NULL,
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL
        SET @FromDate =
            DATEFROMPARTS
            (
                YEAR(GETDATE()),
                MONTH(GETDATE()),
                1
            );

    IF @ToDate IS NULL
        SET @ToDate = CAST(GETDATE() AS DATE);

    IF @TopN <= 0
        SET @TopN = 10;

    SELECT TOP (@TopN)

        p.ProductId,
        p.ProductCode,
        p.ProductName,

        SUM(ed.Quantity) AS TotalExportQuantity,

        CAST(
            SUM(ed.Quantity * ed.UnitPrice)
            AS DECIMAL(18,2)
        ) AS TotalExportValue

    FROM dbo.ExportReceipts AS er

    INNER JOIN dbo.ExportDetails AS ed
        ON ed.ExportReceiptId = er.ExportReceiptId

    INNER JOIN dbo.Products AS p
        ON p.ProductId = ed.ProductId

    WHERE
        er.Status = 'COMPLETED'

        AND er.ReceiptDate >= @FromDate

        AND er.ReceiptDate < DATEADD(DAY, 1, @ToDate)

        AND
        (
            @WarehouseId IS NULL
            OR er.WarehouseId = @WarehouseId
        )

    GROUP BY
        p.ProductId,
        p.ProductCode,
        p.ProductName

    ORDER BY
        TotalExportQuantity DESC,

        TotalExportValue DESC,

        p.ProductName;
END;
GO


/* =========================================================
   4. MONTHLY IMPORT / EXPORT
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_GetMonthlyImportExport
    @FromDate DATE = NULL,
    @ToDate DATE = NULL,
    @WarehouseId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL
        SET @FromDate =
            DATEFROMPARTS
            (
                YEAR(GETDATE()),
                MONTH(GETDATE()),
                1
            );

    IF @ToDate IS NULL
        SET @ToDate = CAST(GETDATE() AS DATE);

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

        WHERE
            ir.Status = 'COMPLETED'

            AND ir.ReceiptDate >= @FromDate

            AND ir.ReceiptDate < DATEADD(DAY, 1, @ToDate)

            AND
            (
                @WarehouseId IS NULL
                OR ir.WarehouseId = @WarehouseId
            )

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

        WHERE
            er.Status = 'COMPLETED'

            AND er.ReceiptDate >= @FromDate

            AND er.ReceiptDate < DATEADD(DAY, 1, @ToDate)

            AND
            (
                @WarehouseId IS NULL
                OR er.WarehouseId = @WarehouseId
            )

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
       AND i.[Month] = e.[Month]

    ORDER BY
        [Year],
        [Month];
END;
GO


/* =========================================================
   5. INVENTORY VALUE
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_GetInventoryValue
    @WarehouseId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        WarehouseId,
        WarehouseCode,
        WarehouseName,

        TotalQuantity,

        InventoryCostValue,

        PotentialSellingValue

    FROM dbo.vw_InventoryValue

    WHERE
        @WarehouseId IS NULL
        OR WarehouseId = @WarehouseId

    ORDER BY
        WarehouseName;
END;
GO


PRINT '06_CreateStoredProcedures.sql completed successfully.';
GO