-- SmartInventoryManager database script skeleton: CreateConstraints
-- Status: LEGACY REFERENCE ONLY. EF Core migrations are the sole schema authority.
-- Do not execute this file to create or alter the database.
USE SmartInventoryManagerDB;
GO

/* =========================================================
   03_CreateConstraints.sql
   SmartInventoryManager
   Database: SmartInventoryManagerDB

   Scope:
   - Primary Keys
   - Unique Constraints
   - Foreign Keys
   - Default Constraints
   - Check Constraints

   NOTE:
   - Run after 02_CreateTables.sql
   - Do NOT recreate PK/UNIQUE indexes in 04_CreateIndexes.sql
   ========================================================= */


/* =========================================================
   1. PRIMARY KEYS
   ========================================================= */

ALTER TABLE Roles
ADD CONSTRAINT PK_Roles
PRIMARY KEY CLUSTERED (RoleId);
GO

ALTER TABLE Users
ADD CONSTRAINT PK_Users
PRIMARY KEY CLUSTERED (UserId);
GO

ALTER TABLE Categories
ADD CONSTRAINT PK_Categories
PRIMARY KEY CLUSTERED (CategoryId);
GO

ALTER TABLE Products
ADD CONSTRAINT PK_Products
PRIMARY KEY CLUSTERED (ProductId);
GO

ALTER TABLE Suppliers
ADD CONSTRAINT PK_Suppliers
PRIMARY KEY CLUSTERED (SupplierId);
GO

ALTER TABLE Warehouses
ADD CONSTRAINT PK_Warehouses
PRIMARY KEY CLUSTERED (WarehouseId);
GO

ALTER TABLE Locations
ADD CONSTRAINT PK_Locations
PRIMARY KEY CLUSTERED (LocationId);
GO

ALTER TABLE Inventory
ADD CONSTRAINT PK_Inventory
PRIMARY KEY CLUSTERED (InventoryId);
GO

ALTER TABLE ImportReceipts
ADD CONSTRAINT PK_ImportReceipts
PRIMARY KEY CLUSTERED (ImportReceiptId);
GO

ALTER TABLE ImportDetails
ADD CONSTRAINT PK_ImportDetails
PRIMARY KEY CLUSTERED (ImportDetailId);
GO

ALTER TABLE ExportReceipts
ADD CONSTRAINT PK_ExportReceipts
PRIMARY KEY CLUSTERED (ExportReceiptId);
GO

ALTER TABLE ExportDetails
ADD CONSTRAINT PK_ExportDetails
PRIMARY KEY CLUSTERED (ExportDetailId);
GO

ALTER TABLE InventoryTransactions
ADD CONSTRAINT PK_InventoryTransactions
PRIMARY KEY CLUSTERED (TransactionId);
GO

ALTER TABLE StockAlerts
ADD CONSTRAINT PK_StockAlerts
PRIMARY KEY CLUSTERED (AlertId);
GO

ALTER TABLE Notifications
ADD CONSTRAINT PK_Notifications
PRIMARY KEY CLUSTERED (NotificationId);
GO

ALTER TABLE SystemLogs
ADD CONSTRAINT PK_SystemLogs
PRIMARY KEY CLUSTERED (LogId);
GO


/* =========================================================
   2. UNIQUE CONSTRAINTS
   ========================================================= */

/* Roles */
ALTER TABLE Roles
ADD CONSTRAINT UQ_Roles_RoleName
UNIQUE (RoleName);
GO


/* Users */
ALTER TABLE Users
ADD CONSTRAINT UQ_Users_Username
UNIQUE (Username);
GO


/* Categories */
ALTER TABLE Categories
ADD CONSTRAINT UQ_Categories_CategoryName
UNIQUE (CategoryName);
GO


/* Products */
ALTER TABLE Products
ADD CONSTRAINT UQ_Products_ProductCode
UNIQUE (ProductCode);
GO


/* Suppliers */
ALTER TABLE Suppliers
ADD CONSTRAINT UQ_Suppliers_SupplierCode
UNIQUE (SupplierCode);
GO


/* Warehouses */
ALTER TABLE Warehouses
ADD CONSTRAINT UQ_Warehouses_WarehouseCode
UNIQUE (WarehouseCode);
GO


/* Locations
   LocationCode only needs to be unique inside a Warehouse.
*/
ALTER TABLE Locations
ADD CONSTRAINT UQ_Locations_Warehouse_LocationCode
UNIQUE (WarehouseId, LocationCode);
GO


/* Inventory
   One Product can have only one current Inventory row
   inside one Warehouse.
*/
ALTER TABLE Inventory
ADD CONSTRAINT UQ_Inventory_Product_Warehouse
UNIQUE (ProductId, WarehouseId);
GO


/* Import Receipts */
ALTER TABLE ImportReceipts
ADD CONSTRAINT UQ_ImportReceipts_ReceiptCode
UNIQUE (ReceiptCode);
GO


/* Import Details */
ALTER TABLE ImportDetails
ADD CONSTRAINT UQ_ImportDetails_Receipt_Product
UNIQUE (ImportReceiptId, ProductId);
GO


/* Export Receipts */
ALTER TABLE ExportReceipts
ADD CONSTRAINT UQ_ExportReceipts_ReceiptCode
UNIQUE (ReceiptCode);
GO


/* Export Details */
ALTER TABLE ExportDetails
ADD CONSTRAINT UQ_ExportDetails_Receipt_Product
UNIQUE (ExportReceiptId, ProductId);
GO


/* =========================================================
   3. FOREIGN KEYS
   ========================================================= */


/* ---------------------------------------------------------
   Users → Roles
   Roles 1:N Users
   --------------------------------------------------------- */
ALTER TABLE Users
ADD CONSTRAINT FK_Users_Roles
FOREIGN KEY (RoleId)
REFERENCES Roles(RoleId);
GO


/* ---------------------------------------------------------
   Products → Categories
   Categories 1:N Products
   --------------------------------------------------------- */
ALTER TABLE Products
ADD CONSTRAINT FK_Products_Categories
FOREIGN KEY (CategoryId)
REFERENCES Categories(CategoryId);
GO


/* ---------------------------------------------------------
   Warehouses → Users
   Users 1:N Warehouses
   ManagerId is nullable.
   --------------------------------------------------------- */
ALTER TABLE Warehouses
ADD CONSTRAINT FK_Warehouses_Manager
FOREIGN KEY (ManagerId)
REFERENCES Users(UserId);
GO


/* ---------------------------------------------------------
   Locations → Warehouses
   Warehouses 1:N Locations
   --------------------------------------------------------- */
ALTER TABLE Locations
ADD CONSTRAINT FK_Locations_Warehouses
FOREIGN KEY (WarehouseId)
REFERENCES Warehouses(WarehouseId);
GO


/* ---------------------------------------------------------
   Inventory → Products
   --------------------------------------------------------- */
ALTER TABLE Inventory
ADD CONSTRAINT FK_Inventory_Products
FOREIGN KEY (ProductId)
REFERENCES Products(ProductId);
GO


/* ---------------------------------------------------------
   Inventory → Warehouses
   --------------------------------------------------------- */
ALTER TABLE Inventory
ADD CONSTRAINT FK_Inventory_Warehouses
FOREIGN KEY (WarehouseId)
REFERENCES Warehouses(WarehouseId);
GO


/* ---------------------------------------------------------
   ImportReceipts → Suppliers
   --------------------------------------------------------- */
ALTER TABLE ImportReceipts
ADD CONSTRAINT FK_ImportReceipts_Suppliers
FOREIGN KEY (SupplierId)
REFERENCES Suppliers(SupplierId);
GO


/* ---------------------------------------------------------
   ImportReceipts → Warehouses
   --------------------------------------------------------- */
ALTER TABLE ImportReceipts
ADD CONSTRAINT FK_ImportReceipts_Warehouses
FOREIGN KEY (WarehouseId)
REFERENCES Warehouses(WarehouseId);
GO


/* ---------------------------------------------------------
   ImportReceipts → Users
   CreatedBy
   --------------------------------------------------------- */
ALTER TABLE ImportReceipts
ADD CONSTRAINT FK_ImportReceipts_CreatedBy
FOREIGN KEY (CreatedBy)
REFERENCES Users(UserId);
GO


/* ---------------------------------------------------------
   ImportDetails → ImportReceipts
   --------------------------------------------------------- */
ALTER TABLE ImportDetails
ADD CONSTRAINT FK_ImportDetails_ImportReceipts
FOREIGN KEY (ImportReceiptId)
REFERENCES ImportReceipts(ImportReceiptId);
GO


/* ---------------------------------------------------------
   ImportDetails → Products
   --------------------------------------------------------- */
ALTER TABLE ImportDetails
ADD CONSTRAINT FK_ImportDetails_Products
FOREIGN KEY (ProductId)
REFERENCES Products(ProductId);
GO


/* ---------------------------------------------------------
   ExportReceipts → Warehouses
   --------------------------------------------------------- */
ALTER TABLE ExportReceipts
ADD CONSTRAINT FK_ExportReceipts_Warehouses
FOREIGN KEY (WarehouseId)
REFERENCES Warehouses(WarehouseId);
GO


/* ---------------------------------------------------------
   ExportReceipts → Users
   CreatedBy
   --------------------------------------------------------- */
ALTER TABLE ExportReceipts
ADD CONSTRAINT FK_ExportReceipts_CreatedBy
FOREIGN KEY (CreatedBy)
REFERENCES Users(UserId);
GO


/* ---------------------------------------------------------
   ExportDetails → ExportReceipts
   --------------------------------------------------------- */
ALTER TABLE ExportDetails
ADD CONSTRAINT FK_ExportDetails_ExportReceipts
FOREIGN KEY (ExportReceiptId)
REFERENCES ExportReceipts(ExportReceiptId);
GO


/* ---------------------------------------------------------
   ExportDetails → Products
   --------------------------------------------------------- */
ALTER TABLE ExportDetails
ADD CONSTRAINT FK_ExportDetails_Products
FOREIGN KEY (ProductId)
REFERENCES Products(ProductId);
GO


/* ---------------------------------------------------------
   InventoryTransactions → Products
   --------------------------------------------------------- */
ALTER TABLE InventoryTransactions
ADD CONSTRAINT FK_InventoryTransactions_Products
FOREIGN KEY (ProductId)
REFERENCES Products(ProductId);
GO


/* ---------------------------------------------------------
   InventoryTransactions → Warehouses
   --------------------------------------------------------- */
ALTER TABLE InventoryTransactions
ADD CONSTRAINT FK_InventoryTransactions_Warehouses
FOREIGN KEY (WarehouseId)
REFERENCES Warehouses(WarehouseId);
GO


/* ---------------------------------------------------------
   InventoryTransactions → Users
   CreatedBy
   --------------------------------------------------------- */
ALTER TABLE InventoryTransactions
ADD CONSTRAINT FK_InventoryTransactions_CreatedBy
FOREIGN KEY (CreatedBy)
REFERENCES Users(UserId);
GO


/* ---------------------------------------------------------
   StockAlerts → Products
   --------------------------------------------------------- */
ALTER TABLE StockAlerts
ADD CONSTRAINT FK_StockAlerts_Products
FOREIGN KEY (ProductId)
REFERENCES Products(ProductId);
GO


/* ---------------------------------------------------------
   StockAlerts → Warehouses
   --------------------------------------------------------- */
ALTER TABLE StockAlerts
ADD CONSTRAINT FK_StockAlerts_Warehouses
FOREIGN KEY (WarehouseId)
REFERENCES Warehouses(WarehouseId);
GO


/* ---------------------------------------------------------
   StockAlerts → Users
   ResolvedBy is nullable.
   --------------------------------------------------------- */
ALTER TABLE StockAlerts
ADD CONSTRAINT FK_StockAlerts_ResolvedBy
FOREIGN KEY (ResolvedBy)
REFERENCES Users(UserId);
GO


/* ---------------------------------------------------------
   Notifications → Users
   --------------------------------------------------------- */
ALTER TABLE Notifications
ADD CONSTRAINT FK_Notifications_Users
FOREIGN KEY (UserId)
REFERENCES Users(UserId);
GO


/* ---------------------------------------------------------
   SystemLogs → Users
   UserId is nullable.
   --------------------------------------------------------- */
ALTER TABLE SystemLogs
ADD CONSTRAINT FK_SystemLogs_Users
FOREIGN KEY (UserId)
REFERENCES Users(UserId);
GO


/* =========================================================
   4. DEFAULT CONSTRAINTS
   ========================================================= */


/* Roles */
ALTER TABLE Roles
ADD CONSTRAINT DF_Roles_Description
DEFAULT NULL FOR Description;
GO


/* Users */
ALTER TABLE Users
ADD CONSTRAINT DF_Users_IsActive
DEFAULT 1 FOR IsActive;
GO

ALTER TABLE Users
ADD CONSTRAINT DF_Users_CreatedAt
DEFAULT SYSDATETIME() FOR CreatedAt;
GO


/* Categories */
ALTER TABLE Categories
ADD CONSTRAINT DF_Categories_IsActive
DEFAULT 1 FOR IsActive;
GO


/* Products */
ALTER TABLE Products
ADD CONSTRAINT DF_Products_PurchasePrice
DEFAULT 0 FOR PurchasePrice;
GO

ALTER TABLE Products
ADD CONSTRAINT DF_Products_SellingPrice
DEFAULT 0 FOR SellingPrice;
GO

ALTER TABLE Products
ADD CONSTRAINT DF_Products_ReorderPoint
DEFAULT 0 FOR ReorderPoint;
GO

ALTER TABLE Products
ADD CONSTRAINT DF_Products_MaximumStock
DEFAULT 0 FOR MaximumStock;
GO

ALTER TABLE Products
ADD CONSTRAINT DF_Products_IsActive
DEFAULT 1 FOR IsActive;
GO

ALTER TABLE Products
ADD CONSTRAINT DF_Products_CreatedAt
DEFAULT SYSDATETIME() FOR CreatedAt;
GO


/* Suppliers */
ALTER TABLE Suppliers
ADD CONSTRAINT DF_Suppliers_IsActive
DEFAULT 1 FOR IsActive;
GO

ALTER TABLE Suppliers
ADD CONSTRAINT DF_Suppliers_CreatedAt
DEFAULT SYSDATETIME() FOR CreatedAt;
GO


/* Warehouses */
ALTER TABLE Warehouses
ADD CONSTRAINT DF_Warehouses_IsActive
DEFAULT 1 FOR IsActive;
GO

ALTER TABLE Warehouses
ADD CONSTRAINT DF_Warehouses_CreatedAt
DEFAULT SYSDATETIME() FOR CreatedAt;
GO


/* Locations */
ALTER TABLE Locations
ADD CONSTRAINT DF_Locations_IsActive
DEFAULT 1 FOR IsActive;
GO


/* Inventory */
ALTER TABLE Inventory
ADD CONSTRAINT DF_Inventory_Quantity
DEFAULT 0 FOR Quantity;
GO

ALTER TABLE Inventory
ADD CONSTRAINT DF_Inventory_UpdatedAt
DEFAULT SYSDATETIME() FOR UpdatedAt;
GO


/* ImportReceipts */
ALTER TABLE ImportReceipts
ADD CONSTRAINT DF_ImportReceipts_ReceiptDate
DEFAULT SYSDATETIME() FOR ReceiptDate;
GO

ALTER TABLE ImportReceipts
ADD CONSTRAINT DF_ImportReceipts_TotalAmount
DEFAULT 0 FOR TotalAmount;
GO

ALTER TABLE ImportReceipts
ADD CONSTRAINT DF_ImportReceipts_Status
DEFAULT 'DRAFT' FOR Status;
GO


/* ExportReceipts */
ALTER TABLE ExportReceipts
ADD CONSTRAINT DF_ExportReceipts_ReceiptDate
DEFAULT SYSDATETIME() FOR ReceiptDate;
GO

ALTER TABLE ExportReceipts
ADD CONSTRAINT DF_ExportReceipts_TotalAmount
DEFAULT 0 FOR TotalAmount;
GO

ALTER TABLE ExportReceipts
ADD CONSTRAINT DF_ExportReceipts_Status
DEFAULT 'DRAFT' FOR Status;
GO


/* InventoryTransactions */
ALTER TABLE InventoryTransactions
ADD CONSTRAINT DF_InventoryTransactions_CreatedAt
DEFAULT SYSDATETIME() FOR CreatedAt;
GO


/* StockAlerts */
ALTER TABLE StockAlerts
ADD CONSTRAINT DF_StockAlerts_IsResolved
DEFAULT 0 FOR IsResolved;
GO

ALTER TABLE StockAlerts
ADD CONSTRAINT DF_StockAlerts_CreatedAt
DEFAULT SYSDATETIME() FOR CreatedAt;
GO


/* Notifications */
ALTER TABLE Notifications
ADD CONSTRAINT DF_Notifications_IsRead
DEFAULT 0 FOR IsRead;
GO

ALTER TABLE Notifications
ADD CONSTRAINT DF_Notifications_CreatedAt
DEFAULT SYSDATETIME() FOR CreatedAt;
GO


/* SystemLogs */
ALTER TABLE SystemLogs
ADD CONSTRAINT DF_SystemLogs_CreatedAt
DEFAULT SYSDATETIME() FOR CreatedAt;
GO


/* =========================================================
   5. CHECK CONSTRAINTS
   ========================================================= */


/* Products */
ALTER TABLE Products
ADD CONSTRAINT CK_Products_PurchasePrice_NonNegative
CHECK (PurchasePrice >= 0);
GO

ALTER TABLE Products
ADD CONSTRAINT CK_Products_SellingPrice_NonNegative
CHECK (SellingPrice >= 0);
GO

ALTER TABLE Products
ADD CONSTRAINT CK_Products_StockLimits
CHECK (
    ReorderPoint >= 0
    AND MaximumStock >= ReorderPoint
);
GO


/* Inventory */
ALTER TABLE Inventory
ADD CONSTRAINT CK_Inventory_Quantity_NonNegative
CHECK (Quantity >= 0);
GO


/* ImportReceipts */
ALTER TABLE ImportReceipts
ADD CONSTRAINT CK_ImportReceipts_TotalAmount_NonNegative
CHECK (TotalAmount >= 0);
GO

ALTER TABLE ImportReceipts
ADD CONSTRAINT CK_ImportReceipts_Status
CHECK (
    Status IN ('DRAFT', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED')
);
GO


/* ImportDetails */
ALTER TABLE ImportDetails
ADD CONSTRAINT CK_ImportDetails_Quantity_Positive
CHECK (Quantity > 0);
GO

ALTER TABLE ImportDetails
ADD CONSTRAINT CK_ImportDetails_UnitPrice_NonNegative
CHECK (UnitPrice >= 0);
GO


/* ExportReceipts */
ALTER TABLE ExportReceipts
ADD CONSTRAINT CK_ExportReceipts_TotalAmount_NonNegative
CHECK (TotalAmount >= 0);
GO

ALTER TABLE ExportReceipts
ADD CONSTRAINT CK_ExportReceipts_Status
CHECK (
    Status IN ('DRAFT', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED')
);
GO


/* ExportDetails */
ALTER TABLE ExportDetails
ADD CONSTRAINT CK_ExportDetails_Quantity_Positive
CHECK (Quantity > 0);
GO

ALTER TABLE ExportDetails
ADD CONSTRAINT CK_ExportDetails_UnitPrice_NonNegative
CHECK (UnitPrice >= 0);
GO


/* InventoryTransactions */
ALTER TABLE InventoryTransactions
ADD CONSTRAINT CK_InventoryTransactions_Quantity_Positive
CHECK (Quantity > 0);
GO

ALTER TABLE InventoryTransactions
ADD CONSTRAINT CK_InventoryTransactions_TransactionType
CHECK (
    TransactionType IN
    (
        'IMPORT',
        'EXPORT',
        'ADJUSTMENT',
        'TRANSFER_IN',
        'TRANSFER_OUT'
    )
);
GO

ALTER TABLE InventoryTransactions
ADD CONSTRAINT CK_InventoryTransactions_ReferenceType
CHECK (
    ReferenceType IS NULL
    OR ReferenceType IN
    (
        'IMPORT_RECEIPT',
        'EXPORT_RECEIPT',
        'MANUAL'
    )
);
GO


/* StockAlerts */
ALTER TABLE StockAlerts
ADD CONSTRAINT CK_StockAlerts_AlertType
CHECK (
    AlertType IN
    (
        'LOW_STOCK',
        'OUT_OF_STOCK',
        'OVERSTOCK'
    )
);
GO

ALTER TABLE StockAlerts
ADD CONSTRAINT CK_StockAlerts_CurrentQuantity
CHECK (CurrentQuantity >= 0);
GO

ALTER TABLE StockAlerts
ADD CONSTRAINT CK_StockAlerts_ResolutionState
CHECK
(
    (IsResolved = 0 AND ResolvedAt IS NULL)
    OR
    (IsResolved = 1 AND ResolvedAt IS NOT NULL)
);
GO


/* =========================================================
   END OF 03_CreateConstraints.sql
   ========================================================= */