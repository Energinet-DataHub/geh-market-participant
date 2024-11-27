CREATE TABLE [dbo].[ActorConsolidationAuditLogEntry](
    [Id]              [INT]              NOT NULL IDENTITY(1, 1),
    [GridAreaId]      [uniqueidentifier] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [Field]           [int]              NOT NULL,
    [OldValue]        [nvarchar](MAX)    NOT NULL,
    [NewValue]        [nvarchar](MAX)    NOT NULL,
    [Timestamp]       [datetimeoffset]   NOT NULL,

    CONSTRAINT PK_ActorConsolidationAuditLogEntry PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_ActorConsolidationAuditLogEntry_GridAreaId_GridArea FOREIGN KEY ([GridAreaId]) REFERENCES [dbo].[GridArea]([Id])
)
GO
