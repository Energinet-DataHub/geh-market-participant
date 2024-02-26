ALTER TABLE [dbo].[UserRoleAssignment]
	ADD Id uniqueidentifier NOT NULL UNIQUE DEFAULT NEWID()
GO
