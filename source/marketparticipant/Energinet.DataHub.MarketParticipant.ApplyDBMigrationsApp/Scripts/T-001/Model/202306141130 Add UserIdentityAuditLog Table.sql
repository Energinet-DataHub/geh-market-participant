CREATE TABLE [dbo].[UserIdentityAuditLogEntry](
    [Id]              [INT]              NOT NULL IDENTITY(1, 1),
    [UserId]          [uniqueidentifier] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [Field]           [int]              NOT NULL,
    [OldValue]        [nvarchar](MAX)    NOT NULL,
    [NewValue]        [nvarchar](MAX)    NOT NULL,
    [Timestamp]       [DATETIMEOFFSET]   NOT NULL,

    CONSTRAINT [PK_UserIdentityAuditLogEntry] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_UserIdentityAuditLogEntry_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserIdentityAuditLogEntry_UserId_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]),
    )
GO
