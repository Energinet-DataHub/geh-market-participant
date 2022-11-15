SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserActor](
    [Id] [uniqueidentifier] NOT NULL,
    [UserId] [uniqueidentifier] NOT NULL,
    [ActorId] [uniqueidentifier] NOT NULL,
     CONSTRAINT [PK_UserActor] PRIMARY KEY CLUSTERED
    (
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

ALTER TABLE [dbo].[UserActor]  WITH CHECK ADD  CONSTRAINT [FK_UserActor_ActorInfoNew] FOREIGN KEY([ActorId])
    REFERENCES [dbo].[ActorInfoNew] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserActor] CHECK CONSTRAINT [FK_UserActor_ActorInfoNew]
    GO

ALTER TABLE [dbo].[UserActor]  WITH CHECK ADD  CONSTRAINT [FK_UserActor_User] FOREIGN KEY([UserId])
    REFERENCES [dbo].[User] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserActor] CHECK CONSTRAINT [FK_UserActor_User]
    GO