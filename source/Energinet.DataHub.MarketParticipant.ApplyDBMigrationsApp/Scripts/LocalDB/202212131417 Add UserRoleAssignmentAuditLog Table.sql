SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleAssignmentAuditLogEntry](
    [Id] [INT] NOT NULL IDENTITY(1, 1),
    [UserId] [uniqueidentifier] NOT NULL,
    [ActorId] [uniqueidentifier] NOT NULL,
    [UserRoleTemplateId] [uniqueidentifier] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [Timestamp] [DATETIMEOFFSET] NOT NULL,
    [AssignmentType] [INT] NOT NULL,

    CONSTRAINT [PK_UserRoleAssignmentAuditLogEntry] PRIMARY KEY CLUSTERED ([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_UserId_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserRoleTemplateId_MarketRoleGridArea FOREIGN KEY ([UserRoleTemplateId]) REFERENCES [dbo].[UserRoleTemplate]([Id])
)
GO