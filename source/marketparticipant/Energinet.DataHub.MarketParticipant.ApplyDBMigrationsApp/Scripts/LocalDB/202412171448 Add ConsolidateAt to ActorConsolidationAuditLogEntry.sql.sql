ALTER TABLE [dbo].[ActorConsolidationAuditLogEntry]
    ADD [ConsolidateAt] [datetimeoffset] NOT NULL DEFAULT (GETUTCDATE())
