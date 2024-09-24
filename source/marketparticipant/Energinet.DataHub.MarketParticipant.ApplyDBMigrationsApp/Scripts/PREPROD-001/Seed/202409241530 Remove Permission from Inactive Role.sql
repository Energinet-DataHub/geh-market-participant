DELETE urp FROM [dbo].[UserRolePermission] urp
JOIN [dbo].[UserRole] ur on urp.UserRoleId = ur.Id
WHERE ur.Status=2
