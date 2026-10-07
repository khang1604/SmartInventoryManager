USE SmartInventoryManagerDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY
	DECLARE @AdminRoleId INT;
	DECLARE @AdminPasswordHash VARCHAR(255) = '$pbkdf2-sha256$600000$g1R4nDagQweC09bb0Rb/DA==$qkoceoTn3pwL8x/HzoGnUS7bdN7DXN9ZtgL359cQwrI=';

	SELECT @AdminRoleId = RoleId
	FROM dbo.Roles
	WHERE RoleName = N'ADMIN';

	IF @AdminRoleId IS NULL
	BEGIN
		INSERT INTO dbo.Roles (RoleName, Description)
		VALUES (N'ADMIN', N'Quản trị hệ thống');

		SET @AdminRoleId = CONVERT(INT, SCOPE_IDENTITY());
	END;

	IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
	BEGIN
		UPDATE dbo.Users
		SET PasswordHash = @AdminPasswordHash,
			FullName = N'Nguyễn Quản Trị',
			Email = 'admin@smartinventory.local',
			RoleId = @AdminRoleId,
			IsActive = 1
		WHERE Username = 'admin';
	END
	ELSE
	BEGIN
		INSERT INTO dbo.Users
		(
			Username,
			PasswordHash,
			FullName,
			Email,
			RoleId,
			IsActive,
			CreatedAt
		)
		VALUES
		(
			'admin',
			@AdminPasswordHash,
			N'Nguyễn Quản Trị',
			'admin@smartinventory.local',
			@AdminRoleId,
			1,
			SYSDATETIME()
		);
	END;

	COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
	BEGIN
		ROLLBACK TRANSACTION;
	END;

	THROW;
END CATCH;
GO

SELECT
	u.UserId,
	u.Username,
	u.Email,
	r.RoleName,
	u.IsActive
FROM dbo.Users AS u
INNER JOIN dbo.Roles AS r ON r.RoleId = u.RoleId
WHERE u.Username = 'admin';
GO
