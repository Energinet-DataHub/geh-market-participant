ALTER TABLE [dbo].[UserRole]
    ADD [Description] nvarchar(max) NULL DEFAULT('')

ALTER TABLE [dbo].[UserRole]
    ADD [Status] [INT] NOT NULL DEFAULT(0)
GO