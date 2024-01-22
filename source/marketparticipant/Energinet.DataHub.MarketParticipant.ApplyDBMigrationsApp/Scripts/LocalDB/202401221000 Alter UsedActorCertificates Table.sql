ALTER TABLE [dbo].[UsedActorCertificates]
    ADD [ActorId] [uniqueidentifier] NULL
    CONSTRAINT [FK_UsedActorCertificates_Actor] FOREIGN KEY ([ActorId]) REFERENCES [dbo].[Actor] ([Id])
GO

UPDATE [dbo].[UsedActorCertificates]
SET [ActorId] = [dbo].[ActorCertificateCredentials].[ActorId]
FROM [dbo].[UsedActorCertificates]
         INNER JOIN [dbo].[ActorCertificateCredentials] ON [dbo].[UsedActorCertificates].[Thumbprint] = [dbo].[ActorCertificateCredentials].[Thumbprint]

ALTER TABLE [dbo].[UsedActorCertificates]
    ALTER COLUMN [ActorId] [uniqueidentifier] NOT NULL

GO