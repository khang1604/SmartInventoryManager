-- SmartInventoryManager database script skeleton: CreateTables
-- Status: LEGACY REFERENCE ONLY. EF Core migrations are the sole schema authority.
-- Do not execute this file to create or alter the database.
USE SmartInventoryManagerDB;
GO

/* =========================================================
   02_CreateTables.sql

   SmartInventoryManager
   Create 16 Tables

   IMPORTANT:
   - No FK constraints in this file.
   - No UNIQUE constraints in this file.
   - No CHECK constraints in this file.
   - No indexes in this file.
   - Those will be handled in later scripts.
   ========================================================= */


/* =========================================================
   1. ROLES
   ========================================================= */

CREATE TABLE Roles
(
    RoleId INT IDENTITY(1,1) NOT NULL,
    RoleName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NULL
);
GO


/* =========================================================
   2. USERS
   ========================================================= */

CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) NOT NULL,
    Username VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NULL,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   3. CATEGORIES
   ========================================================= */

CREATE TABLE Categories
(
    CategoryId INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL
);
GO


/* =========================================================
   4. PRODUCTS
   ========================================================= */

CREATE TABLE Products
(
    ProductId INT IDENTITY(1,1) NOT NULL,
    ProductCode VARCHAR(50) NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    CategoryId INT NOT NULL,
    Unit NVARCHAR(30) NOT NULL,
    PurchasePrice DECIMAL(18,2) NOT NULL,
    SellingPrice DECIMAL(18,2) NOT NULL,
    ReorderPoint INT NOT NULL,
    MaximumStock INT NOT NULL,
    IsActive BIT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   5. SUPPLIERS
   ========================================================= */

CREATE TABLE Suppliers
(
    SupplierId INT IDENTITY(1,1) NOT NULL,
    SupplierCode VARCHAR(50) NOT NULL,
    SupplierName NVARCHAR(200) NOT NULL,
    Phone VARCHAR(20) NULL,
    Email VARCHAR(100) NULL,
    Address NVARCHAR(255) NULL,
    TaxCode VARCHAR(50) NULL,
    IsActive BIT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   6. WAREHOUSES
   ========================================================= */

CREATE TABLE Warehouses
(
    WarehouseId INT IDENTITY(1,1) NOT NULL,
    WarehouseCode VARCHAR(50) NOT NULL,
    WarehouseName NVARCHAR(200) NOT NULL,
    Location NVARCHAR(255) NULL,
    ManagerId INT NULL,
    IsActive BIT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   7. LOCATIONS
   ========================================================= */

CREATE TABLE Locations
(
    LocationId INT IDENTITY(1,1) NOT NULL,
    WarehouseId INT NOT NULL,
    LocationCode VARCHAR(50) NOT NULL,
    LocationName NVARCHAR(100) NULL,
    IsActive BIT NOT NULL
);
GO


/* =========================================================
   8. INVENTORY
   ========================================================= */

CREATE TABLE Inventory
(
    InventoryId INT IDENTITY(1,1) NOT NULL,
    ProductId INT NOT NULL,
    WarehouseId INT NOT NULL,
    Quantity INT NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   9. IMPORT RECEIPTS
   ========================================================= */

CREATE TABLE ImportReceipts
(
    ImportReceiptId INT IDENTITY(1,1) NOT NULL,
    ReceiptCode VARCHAR(50) NOT NULL,
    SupplierId INT NOT NULL,
    WarehouseId INT NOT NULL,
    CreatedBy INT NOT NULL,
    ReceiptDate DATETIME2 NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(20) NOT NULL,
    Note NVARCHAR(500) NULL
);
GO


/* =========================================================
   10. IMPORT DETAILS
   ========================================================= */

CREATE TABLE ImportDetails
(
    ImportDetailId INT IDENTITY(1,1) NOT NULL,
    ImportReceiptId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL
);
GO


/* =========================================================
   11. EXPORT RECEIPTS
   ========================================================= */

CREATE TABLE ExportReceipts
(
    ExportReceiptId INT IDENTITY(1,1) NOT NULL,
    ReceiptCode VARCHAR(50) NOT NULL,
    WarehouseId INT NOT NULL,
    CreatedBy INT NOT NULL,
    ReceiptDate DATETIME2 NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(20) NOT NULL,
    Note NVARCHAR(500) NULL
);
GO


/* =========================================================
   12. EXPORT DETAILS
   ========================================================= */

CREATE TABLE ExportDetails
(
    ExportDetailId INT IDENTITY(1,1) NOT NULL,
    ExportReceiptId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL
);
GO


/* =========================================================
   13. INVENTORY TRANSACTIONS
   ========================================================= */

CREATE TABLE InventoryTransactions
(
    TransactionId BIGINT IDENTITY(1,1) NOT NULL,
    ProductId INT NOT NULL,
    WarehouseId INT NOT NULL,
    TransactionType VARCHAR(30) NOT NULL,
    Quantity INT NOT NULL,
    ReferenceType VARCHAR(30) NULL,
    ReferenceId INT NULL,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    Note NVARCHAR(500) NULL
);
GO


/* =========================================================
   14. STOCK ALERTS
   ========================================================= */

CREATE TABLE StockAlerts
(
    AlertId BIGINT IDENTITY(1,1) NOT NULL,
    ProductId INT NOT NULL,
    WarehouseId INT NOT NULL,
    AlertType VARCHAR(30) NOT NULL,
    CurrentQuantity INT NOT NULL,
    Threshold INT NULL,
    IsResolved BIT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ResolvedAt DATETIME2 NULL,
    ResolvedBy INT NULL
);
GO


/* =========================================================
   15. NOTIFICATIONS
   ========================================================= */

CREATE TABLE Notifications
(
    NotificationId BIGINT IDENTITY(1,1) NOT NULL,
    UserId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    NotificationType VARCHAR(30) NULL,
    IsRead BIT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO


/* =========================================================
   16. SYSTEM LOGS
   ========================================================= */

CREATE TABLE SystemLogs
(
    LogId BIGINT IDENTITY(1,1) NOT NULL,
    UserId INT NULL,
    Action VARCHAR(100) NOT NULL,
    EntityName VARCHAR(100) NULL,
    EntityId INT NULL,
    Description NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO

SELECT COUNT(*) AS TableCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';

SELECT COUNT(*) AS SmartInventoryTableCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_NAME <> 'sysdiagrams';

EXEC sp_help 'Products';