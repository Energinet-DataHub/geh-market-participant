EXEC sp_rename 'dbo.UserRoleAssignment.TemplateId', 'UserRoleId', 'COLUMN';
EXEC sp_rename 'dbo.UserRoleTemplateEicFunction.UserRoleTemplateId', 'UserRoleId', 'COLUMN';
EXEC sp_rename 'dbo.UserRoleTemplatePermission.UserRoleTemplateId', 'UserRoleId', 'COLUMN';

EXEC sp_rename 'dbo.UserRoleTemplateEicFunction', 'UserRoleEicFunction';
EXEC sp_rename 'dbo.UserRoleTemplatePermission', 'UserRolePermission';
EXEC sp_rename 'dbo.UserRoleTemplate', 'UserRole';
