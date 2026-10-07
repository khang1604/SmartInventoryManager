USE SmartInventoryManagerDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @StaffRoleId INT =
(
	SELECT RoleId
	FROM dbo.Roles
	WHERE RoleName = N'STAFF'
);

IF @StaffRoleId IS NULL
BEGIN
	RAISERROR('Role STAFF does not exist. Run the role authentication configuration first.', 16, 1);
	RETURN;
END;

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
SELECT source.Username,
	   source.PasswordHash,
	   source.FullName,
	   source.Email,
	   @StaffRoleId,
	   1,
	   SYSDATETIME()
FROM
(
	SELECT
		'user1' AS Username,
		'$pbkdf2-sha256$600000$nUgixaubXeM5aVaS8wSUSA==$zBG67lzJQ1jwI7TXHBb9f2rxrIkm+oKlROYh66pE/5o=' AS PasswordHash,
		N'Người dùng 1' AS FullName,
		'user1@gmail.com' AS Email
	UNION ALL
	SELECT
		'user2',
		'$pbkdf2-sha256$600000$MrukiUIk1MmDbMFeZ6sluQ==$6QMMsRgsv6vOFpLP3lbcSarrru85T5M0hxBLdl4KEfo=',
		N'Người dùng 2',
		'user2@gmail.com'
	UNION ALL
	SELECT
		'user3',
		'$pbkdf2-sha256$600000$cciBgovDijDiWpxoSHzVLQ==$Qz28jcNFXvSccXIEkkWpln3e0ylsyrx2nywPTAC1UvE=',
		N'Người dùng 3',
		'user3@gmail.com'
) AS source
WHERE NOT EXISTS
(
	SELECT 1
	FROM dbo.Users existing
	WHERE existing.Email = source.Email
	   OR existing.Username = source.Username
);

COMMIT TRANSACTION;
GO

SELECT u.UserId,
	   u.Username,
	   u.Email,
	   r.RoleName,
	   u.IsActive
FROM dbo.Users AS u
INNER JOIN dbo.Roles AS r ON r.RoleId = u.RoleId
WHERE u.Email IN ('user1@gmail.com', 'user2@gmail.com', 'user3@gmail.com')
ORDER BY u.Email;
GO
