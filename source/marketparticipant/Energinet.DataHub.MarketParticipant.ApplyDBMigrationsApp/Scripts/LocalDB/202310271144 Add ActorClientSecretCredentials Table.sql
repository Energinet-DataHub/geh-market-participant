CREATE TABLE [dbo].[ActorClientSecretCredentials]
(
    [ActorId]           [uniqueidentifier]   NOT NULL,
    [ClientSecretId]    [nvarchar](250)      NOT NULL
    CONSTRAINT [UQ_ActorClientSecretCredentials_SecretIdentifier] UNIQUE NONCLUSTERED
(
[ClientSecretId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]