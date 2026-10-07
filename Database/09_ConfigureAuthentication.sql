USE SmartInventoryManagerDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'ADMIN')
BEGIN
	INSERT INTO dbo.Roles (RoleName, Description)
	VALUES (N'ADMIN', N'Quản trị hệ thống');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'WAREHOUSE_MANAGER')
BEGIN
	INSERT INTO dbo.Roles (RoleName, Description)
	VALUES (N'WAREHOUSE_MANAGER', N'Quản lý kho');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'STAFF')
BEGIN
	INSERT INTO dbo.Roles (RoleName, Description)
	VALUES (N'STAFF', N'Nhân viên kho');
END;

UPDATE dbo.Users
SET Email = CASE Username
	WHEN 'admin' THEN 'admin@smartinventory.local'
	WHEN 'manager' THEN 'manager@smartinventory.local'
	WHEN 'staff' THEN 'staff@smartinventory.local'
	ELSE Email
END
WHERE Email IS NULL
  AND Username IN ('admin', 'manager', 'staff');

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email IS NULL)
BEGIN
	RAISERROR('Cannot make Users.Email required because existing users have no email.', 16, 1);
	RETURN;
END;

IF NOT EXISTS
(
	SELECT 1
	FROM sys.key_constraints
	WHERE parent_object_id = OBJECT_ID(N'dbo.Users')
	  AND name = N'UQ_Users_Email'
)
BEGIN
	ALTER TABLE dbo.Users
	ADD CONSTRAINT UQ_Users_Email UNIQUE (Email);
END;

COMMIT TRANSACTION;
GO

SELECT RoleId, RoleName, Description
FROM dbo.Roles
WHERE RoleName IN (N'ADMIN', N'WAREHOUSE_MANAGER', N'STAFF')
ORDER BY RoleName;

SELECT UserId, Username, Email, RoleId, IsActive
FROM dbo.Users
ORDER BY UserId;
GO
