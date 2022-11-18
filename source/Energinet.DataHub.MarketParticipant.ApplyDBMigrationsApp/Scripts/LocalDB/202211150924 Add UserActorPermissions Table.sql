SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserActorPermissions](
    [Id] [uniqueidentifier] NOT NULL,
    [UserActorId] [uniqueidentifier] NOT NULL,
    [PermissionsId] [nvarchar](250) NOT NULL,
    [PermissionsKey] [nvarchar](250) NOT NULL,
     CONSTRAINT [PK_UserActorUserRole] PRIMARY KEY CLUSTERED
    (
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

ALTER TABLE [dbo].[UserActorPermissions]  WITH CHECK ADD  CONSTRAINT [FK_UserActorPermissions_UserActor] FOREIGN KEY([UserActorId])
    REFERENCES [dbo].[UserActor] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserActorPermissions] CHECK CONSTRAINT [FK_UserActorPermissions_UserActor]
    GO

ALTER TABLE [dbo].[UserActorPermissions]  WITH CHECK ADD  CONSTRAINT [FK_UserActorPermissions_AccessPermission] FOREIGN KEY([PermissionsId])
    REFERENCES [dbo].[AccessPermission] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserActorPermissions] CHECK CONSTRAINT [FK_UserActorPermissions_UserActor]
    GO