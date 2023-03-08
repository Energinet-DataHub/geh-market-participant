ALTER TABLE [dbo].[Permission]
    ADD [Created] [DATETIMEOFFSET] NULL
GO

UPDATE [dbo].[Permission] SET [Created] = GETUTCDATE()
GO

ALTER TABLE [dbo].[Permission] ALTER COLUMN [Created] [DATETIMEOFFSET] NOT NULL;
GO