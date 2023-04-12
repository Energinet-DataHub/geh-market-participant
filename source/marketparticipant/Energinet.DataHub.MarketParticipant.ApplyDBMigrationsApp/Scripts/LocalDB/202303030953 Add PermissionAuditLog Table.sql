SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PermissionAuditLogEntry](
    [EntryId] [INT] NOT NULL IDENTITY(1, 1),
    [PermissionId] [INT] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [PermissionChangeType] [INT] NOT NULL,
    [Timestamp] [DATETIMEOFFSET] NOT NULL,

    CONSTRAINT [PK_PermissionAuditLogEntry] PRIMARY KEY CLUSTERED ([EntryId] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
    
    CONSTRAINT FK_PermissionAuditLogEntry_PermissionId_Permission FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permission]([Id]),
    CONSTRAINT FK_PermissionAuditLogEntry_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    )
GO