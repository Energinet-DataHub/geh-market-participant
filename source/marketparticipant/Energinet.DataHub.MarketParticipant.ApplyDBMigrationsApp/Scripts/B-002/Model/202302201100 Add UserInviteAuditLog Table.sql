SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserInviteAuditLogEntry](
    [Id] [INT] NOT NULL IDENTITY(1, 1),
    [UserId] [uniqueidentifier] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [ActorId] [uniqueidentifier] NOT NULL,
    [Timestamp] [DATETIMEOFFSET] NOT NULL,

    CONSTRAINT [PK_UserInviteAuditLogEntry] PRIMARY KEY CLUSTERED ([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_UserInviteAuditLogEntry_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserInviteAuditLogEntry_UserId_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserInviteAuditLogEntry_ActorId_Actor FOREIGN KEY ([ActorId]) REFERENCES [dbo].[ActorInfoNew]([Id]),
    )
GO