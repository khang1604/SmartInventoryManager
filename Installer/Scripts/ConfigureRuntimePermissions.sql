-- Run as deployment administrator on the confirmed Express instance.
-- Creates no login/password and grants no schema or server administration rights.
USE [SmartInventoryManagerProductionDB];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF CONVERT(nvarchar(128), SERVERPROPERTY('InstanceName')) <> N'SQLEXPRESS'
    THROW 51000, 'Expected the confirmed SQLEXPRESS instance.', 1;
IF DB_NAME() <> N'SmartInventoryManagerProductionDB'
    THROW 51000, 'Unexpected database.', 1;
IF SUSER_ID(N'vioware_app') IS NULL
    THROW 51000, 'Create vioware_app interactively before granting permissions.', 1;
IF USER_ID(N'vioware_app') IS NOT NULL
   AND (SELECT sid FROM sys.database_principals WHERE name = N'vioware_app') <> SUSER_SID(N'vioware_app')
    THROW 51000, 'Existing database user is mapped to a different login; review manually.', 1;

BEGIN TRANSACTION;
IF USER_ID(N'vioware_app') IS NULL
    CREATE USER [vioware_app] FOR LOGIN [vioware_app] WITH DEFAULT_SCHEMA = [dbo];
IF DATABASE_PRINCIPAL_ID(N'VioWareRuntime') IS NULL
    CREATE ROLE [VioWareRuntime] AUTHORIZATION [dbo];
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members
    WHERE role_principal_id = DATABASE_PRINCIPAL_ID(N'VioWareRuntime')
      AND member_principal_id = USER_ID(N'vioware_app'))
    ALTER ROLE [VioWareRuntime] ADD MEMBER [vioware_app];

-- Existing services read these 19 business tables. No access to migration history.
DECLARE @permissions TABLE (TableName sysname PRIMARY KEY, PermissionList nvarchar(100));
INSERT INTO @permissions VALUES
    (N'Roles', N'SELECT'),
    (N'Categories', N'SELECT, INSERT, UPDATE'),
    (N'Suppliers', N'SELECT, INSERT, UPDATE'),
    (N'Warehouses', N'SELECT, INSERT, UPDATE'),
    (N'Locations', N'SELECT, INSERT, UPDATE'),
    (N'Products', N'SELECT, INSERT, UPDATE'),
    (N'Inventory', N'SELECT, INSERT, UPDATE'),
    (N'StockAlerts', N'SELECT, INSERT, UPDATE'),
    (N'Users', N'SELECT, INSERT, UPDATE'),
    (N'UserWarehouses', N'SELECT, INSERT, UPDATE'),
    (N'Notifications', N'SELECT, INSERT, UPDATE'),
    (N'ImportReceipts', N'SELECT, INSERT, UPDATE, DELETE'),
    (N'ExportReceipts', N'SELECT, INSERT, UPDATE, DELETE'),
    (N'ImportDetails', N'SELECT, INSERT, UPDATE, DELETE'),
    (N'ExportDetails', N'SELECT, INSERT, UPDATE, DELETE'),
    (N'InventoryTransactions', N'SELECT, INSERT'),
    (N'ReceiptRejections', N'SELECT, INSERT'),
    (N'ReceiptStatusChanges', N'SELECT, INSERT'),
    (N'SystemLogs', N'SELECT, INSERT');

DECLARE @table sysname, @permissionList nvarchar(100), @sql nvarchar(max);
DECLARE permission_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT TableName, PermissionList FROM @permissions;
OPEN permission_cursor;
FETCH NEXT FROM permission_cursor INTO @table, @permissionList;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + @table, N'U') IS NULL
        THROW 51000, 'Missing business table; apply and review migrations first.', 1;
    SET @sql = N'GRANT ' + @permissionList + N' ON OBJECT::dbo.' + QUOTENAME(@table) + N' TO [VioWareRuntime];';
    EXEC sys.sp_executesql @sql;
    FETCH NEXT FROM permission_cursor INTO @table, @permissionList;
END;
CLOSE permission_cursor;
DEALLOCATE permission_cursor;

-- NEXT VALUE FOR requires UPDATE permission on this sequence.
GRANT UPDATE ON OBJECT::dbo.TransferReferenceSequence TO [VioWareRuntime];
COMMIT;
PRINT 'Runtime permissions configured; existing unrelated grants were not removed.';
