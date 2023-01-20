SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleAuditLogEntry](
    [Id] [INT] NOT NULL IDENTITY(1, 1),
    [UserRoleId] [uniqueidentifier] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [UserRoleChangeType] [INT] NOT NULL,
    [ChangeDescriptionJson] NVARCHAR(MAX) NULL,
    [Timestamp] [DATETIMEOFFSET] NOT NULL,

    CONSTRAINT [PK_UserRoleAuditLogEntry] PRIMARY KEY CLUSTERED ([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_UserRoleAuditLogEntry_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserRoleAuditLogEntry_UserRoleId_UserRole FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRole]([Id])
)
GO