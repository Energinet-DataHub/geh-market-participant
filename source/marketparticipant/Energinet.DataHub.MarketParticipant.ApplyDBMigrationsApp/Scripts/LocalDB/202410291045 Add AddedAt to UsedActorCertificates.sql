ALTER TABLE [dbo].[UsedActorCertificates]
    ADD [AddedAt] [datetimeoffset] NOT NULL DEFAULT (GETUTCDATE())
