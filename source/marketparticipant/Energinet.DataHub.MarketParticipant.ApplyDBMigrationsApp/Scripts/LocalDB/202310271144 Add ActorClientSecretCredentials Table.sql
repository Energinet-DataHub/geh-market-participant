CREATE TABLE [dbo].[ActorClientSecretCredentials]
(
    [Id]            [int] IDENTITY(1, 1) NOT NULL,
    [ActorId]       [uniqueidentifier]   NOT NULL,
    CONSTRAINT [PK_ActorClientSecretCredentials_Id] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]