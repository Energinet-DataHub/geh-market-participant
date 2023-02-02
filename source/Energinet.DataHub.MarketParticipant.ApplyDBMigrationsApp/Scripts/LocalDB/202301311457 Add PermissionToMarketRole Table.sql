SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PermissionEicFunction](
    [Id] [uniqueidentifier] NOT NULL,
    [PermissionId] [INT] NOT NULL,
    [EicFunction] [INT] NOT NULL,
    CONSTRAINT [PK_PermissionEicFunction] PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

ALTER TABLE [dbo].[PermissionEicFunction]  WITH CHECK ADD  CONSTRAINT [FK_PermissionEicFunction_Permission] FOREIGN KEY([PermissionId])
    REFERENCES [dbo].[Permission] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[PermissionEicFunction] CHECK CONSTRAINT [FK_PermissionEicFunction_Permission]
    GO