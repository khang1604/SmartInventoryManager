-- SmartInventoryManager database script skeleton: CreateIndexes
-- Status: not implemented. Do not execute or use this file to create a database.
USE SmartInventoryManagerDB;
GO

/* =========================================================
   04_CreateIndexes.sql
   SmartInventoryManager

   Purpose:
   - Nonclustered indexes for JOIN / WHERE / ORDER BY
   - Search/list indexes
   - Reporting/history indexes

   IMPORTANT:
   PK and UNIQUE indexes are NOT recreated here.
   They were already created by constraints in 03_CreateConstraints.sql.
   ========================================================= */


/* =========================================================
   1. USERS
   ========================================================= */

/*
   Role-based queries:

   SELECT *
   FROM Users
   WHERE RoleId = @RoleId
*/
CREATE NONCLUSTERED INDEX IX_Users_RoleId
ON Users (RoleId);
GO


/* =========================================================
   2. PRODUCTS
   ========================================================= */

/*
   Category filtering:

   WHERE CategoryId = @CategoryId
*/
CREATE NONCLUSTERED INDEX IX_Products_CategoryId
ON Products (CategoryId);
GO


/*
   Product name list/search/sort support.

   IMPORTANT:
   This index does NOT magically make:

       LIKE '%' + @Keyword + '%'

   become an index seek.

   It is still useful for:
   - exact/prefix search
   - ORDER BY ProductName
   - product list queries
   - other predicates involving ProductName

   Substring search with a leading '%' can still require
   scanning the index/table.
*/
CREATE NONCLUSTERED INDEX IX_Products_ProductName
ON Products (ProductName);
GO


/* =========================================================
   3. SUPPLIERS
   ========================================================= */

/*
   Supplier list/search/sort.
*/
CREATE NONCLUSTERED INDEX IX_Suppliers_SupplierName
ON Suppliers (SupplierName);
GO


/* =========================================================
   4. WAREHOUSES
   ========================================================= */

/*
   Warehouse list/search/sort.
*/
CREATE NONCLUSTERED INDEX IX_Warehouses_WarehouseName
ON Warehouses (WarehouseName);
GO


/*
   Warehouse manager lookup.
*/
CREATE NONCLUSTERED INDEX IX_Warehouses_ManagerId
ON Warehouses (ManagerId);
GO


/* =========================================================
   5. LOCATIONS
   ========================================================= */

/*
   The UNIQUE constraint:

       (WarehouseId, LocationCode)

   already creates an index whose leading column is WarehouseId.

   Therefore we DO NOT create another simple WarehouseId index here.
*/


/* =========================================================
   6. INVENTORY
   ========================================================= */

/*
   Current stock by warehouse:

       WHERE WarehouseId = @WarehouseId

   The UNIQUE index:

       (ProductId, WarehouseId)

   is excellent for Product + Warehouse,
   but not ideal when searching by WarehouseId alone.

   Therefore this additional index is justified.
*/
CREATE NONCLUSTERED INDEX IX_Inventory_WarehouseId
ON Inventory (WarehouseId);
GO


/* =========================================================
   7. IMPORT RECEIPTS
   ========================================================= */

/*
   Warehouse + date is a common inventory/reporting query:

       WHERE WarehouseId = @WarehouseId
       ORDER BY ReceiptDate DESC
*/
CREATE NONCLUSTERED INDEX IX_ImportReceipts_Warehouse_ReceiptDate
ON ImportReceipts
(
    WarehouseId,
    ReceiptDate DESC
);
GO


/*
   Supplier history:

       WHERE SupplierId = @SupplierId
       ORDER BY ReceiptDate DESC
*/
CREATE NONCLUSTERED INDEX IX_ImportReceipts_Supplier_ReceiptDate
ON ImportReceipts
(
    SupplierId,
    ReceiptDate DESC
);
GO


/* =========================================================
   8. IMPORT DETAILS
   ========================================================= */

/*
   Product import history:

       WHERE ProductId = @ProductId
*/
CREATE NONCLUSTERED INDEX IX_ImportDetails_ProductId
ON ImportDetails (ProductId);
GO


/* =========================================================
   9. EXPORT RECEIPTS
   ========================================================= */

/*
   Warehouse + date:

       WHERE WarehouseId = @WarehouseId
       ORDER BY ReceiptDate DESC
*/
CREATE NONCLUSTERED INDEX IX_ExportReceipts_Warehouse_ReceiptDate
ON ExportReceipts
(
    WarehouseId,
    ReceiptDate DESC
);
GO


/* =========================================================
   10. EXPORT DETAILS
   ========================================================= */

/*
   Product export history:

       WHERE ProductId = @ProductId
*/
CREATE NONCLUSTERED INDEX IX_ExportDetails_ProductId
ON ExportDetails (ProductId);
GO


/* =========================================================
   11. INVENTORY TRANSACTIONS
   ========================================================= */

/*
   Warehouse + Product + time:

   Useful for:
   - warehouse inventory history
   - product history inside a warehouse
   - transaction reports
*/
CREATE NONCLUSTERED INDEX IX_InventoryTransactions_Warehouse_Product_CreatedAt
ON InventoryTransactions
(
    WarehouseId,
    ProductId,
    CreatedAt DESC
);
GO


/*
   Product-wide history:

       WHERE ProductId = @ProductId
       ORDER BY CreatedAt DESC

   This is intentionally separate because the previous
   composite index starts with WarehouseId.
*/
CREATE NONCLUSTERED INDEX IX_InventoryTransactions_Product_CreatedAt
ON InventoryTransactions
(
    ProductId,
    CreatedAt DESC
);
GO


/* =========================================================
   12. STOCK ALERTS
   ========================================================= */

/*
   Dashboard / alert screen commonly needs:

       WHERE WarehouseId = @WarehouseId
       AND IsResolved = 0
       ORDER BY CreatedAt DESC
*/
CREATE NONCLUSTERED INDEX IX_StockAlerts_Warehouse_Resolved_CreatedAt
ON StockAlerts
(
    WarehouseId,
    IsResolved,
    CreatedAt DESC
);
GO


/* =========================================================
   13. NOTIFICATIONS
   ========================================================= */

/*
   Notification panel commonly needs:

       WHERE UserId = @UserId
       AND IsRead = 0
       ORDER BY CreatedAt DESC
*/
CREATE NONCLUSTERED INDEX IX_Notifications_User_Read_CreatedAt
ON Notifications
(
    UserId,
    IsRead,
    CreatedAt DESC
);
GO


/* =========================================================
   14. SYSTEM LOGS
   ========================================================= */

/*
   Audit/log screen:

       ORDER BY CreatedAt DESC
*/
CREATE NONCLUSTERED INDEX IX_SystemLogs_CreatedAt
ON SystemLogs
(
    CreatedAt DESC
);
GO


/*
   User-specific audit history:

       WHERE UserId = @UserId
       ORDER BY CreatedAt DESC
*/
CREATE NONCLUSTERED INDEX IX_SystemLogs_User_CreatedAt
ON SystemLogs
(
    UserId,
    CreatedAt DESC
);
GO


/* =========================================================
   END OF 04_CreateIndexes.sql
   ========================================================= */