SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserActorUserRole](
    [Id] [uniqueidentifier] NOT NULL,
    [UserActorId] [uniqueidentifier] NOT NULL,
    [UserRoleTemplateId] [uniqueidentifier] NOT NULL,
     CONSTRAINT [PK_UserActorUserRole] PRIMARY KEY CLUSTERED
    (
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

ALTER TABLE [dbo].[UserActorUserRole]  WITH CHECK ADD  CONSTRAINT [FK_UserActorUserRole_UserActor] FOREIGN KEY([UserActorId])
    REFERENCES [dbo].[UserActor] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserActorUserRole] CHECK CONSTRAINT [FK_UserActorUserRole_UserActor]
    GO

ALTER TABLE [dbo].[UserActorUserRole]  WITH CHECK ADD  CONSTRAINT [FK_UserActorUserRole_UserRoleTemplate] FOREIGN KEY([UserRoleTemplateId])
    REFERENCES [dbo].[UserRoleTemplate] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserActorUserRole] CHECK CONSTRAINT [FK_UserActorUserRole_UserRoleTemplate]
    GO