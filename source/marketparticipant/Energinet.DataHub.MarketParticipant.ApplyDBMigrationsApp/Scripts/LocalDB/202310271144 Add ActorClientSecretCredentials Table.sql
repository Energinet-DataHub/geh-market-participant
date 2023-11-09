CREATE TABLE [dbo].[ActorClientSecretCredentials]
(
    [ActorId]           [uniqueidentifier]   NOT NULL,
    [ClientSecretId]    [nvarchar](250)      NOT NULL
    CONSTRAINT [UQ_ActorClientSecretCredentials_SecretIdentifier] UNIQUE NONCLUSTERED
) ON [PRIMARY]