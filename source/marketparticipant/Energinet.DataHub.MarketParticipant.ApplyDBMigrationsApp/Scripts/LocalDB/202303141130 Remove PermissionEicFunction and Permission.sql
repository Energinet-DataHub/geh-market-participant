ALTER TABLE [dbo].[PermissionAuditLogEntry]
DROP CONSTRAINT [FK_PermissionAuditLogEntry_PermissionId_Permission]
GO

ALTER TABLE [dbo].[Permission]
DROP COLUMN [Created]
GO

DROP Table [dbo].[PermissionEicFunction]
GO
