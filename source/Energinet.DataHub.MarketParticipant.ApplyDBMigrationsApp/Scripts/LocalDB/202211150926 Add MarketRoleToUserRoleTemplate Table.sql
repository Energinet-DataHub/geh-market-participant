SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MarketRoleToUserRoleTemplate](
    [Id] [uniqueidentifier] NOT NULL,
    [UserRoleTemplateId] [uniqueidentifier] NOT NULL,
    [Function] [int] NOT NULL,
     CONSTRAINT [PK_MarketRoleToUserRoleTemplate] PRIMARY KEY CLUSTERED
    (
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

ALTER TABLE [dbo].[MarketRoleToUserRoleTemplate]  WITH CHECK ADD  CONSTRAINT [FK_MarketRoleToUserRoleTemplate_UserRoleTemplate] FOREIGN KEY([UserRoleTemplateId])
    REFERENCES [dbo].[UserRoleTemplate] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[MarketRoleToUserRoleTemplate] CHECK CONSTRAINT [FK_MarketRoleToUserRoleTemplate_UserRoleTemplate]
    GO