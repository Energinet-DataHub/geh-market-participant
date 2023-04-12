SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleAssignment](
    [UserId] [uniqueidentifier] NOT NULL,
    [ActorId] [uniqueidentifier] NOT NULL,
    [TemplateId] [uniqueidentifier] NOT NULL,
     CONSTRAINT [PK_UserRoleAssignment] PRIMARY KEY CLUSTERED
    (
    [UserId] ASC,
    [ActorId] ASC,
[TemplateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO