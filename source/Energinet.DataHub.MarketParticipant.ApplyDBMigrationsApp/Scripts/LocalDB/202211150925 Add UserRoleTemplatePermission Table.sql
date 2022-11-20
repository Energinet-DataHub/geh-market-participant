SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleTemplatePermission](
    [Id] [uniqueidentifier] NOT NULL,
    [UserRoleTemplateId] [uniqueidentifier] NOT NULL,
    [PermissionId] [nvarchar](250) NOT NULL,
    CONSTRAINT [PK_UserRoleTemplatePermission] PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

ALTER TABLE [dbo].[UserRoleTemplatePermission]  WITH CHECK ADD  CONSTRAINT [FK_UserRoleTemplatePermission_AccessPermission] FOREIGN KEY([PermissionId])
    REFERENCES [dbo].[AccessPermission] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserRoleTemplatePermission] CHECK CONSTRAINT [FK_UserRoleTemplatePermission_AccessPermission]
    GO

ALTER TABLE [dbo].[UserRoleTemplatePermission]  WITH CHECK ADD  CONSTRAINT [FK_UserRoleTemplatePermission_UserRoleTemplate] FOREIGN KEY([UserRoleTemplateId])
    REFERENCES [dbo].[UserRoleTemplate] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserRoleTemplatePermission] CHECK CONSTRAINT [FK_UserRoleTemplatePermission_UserRoleTemplate]
    GO