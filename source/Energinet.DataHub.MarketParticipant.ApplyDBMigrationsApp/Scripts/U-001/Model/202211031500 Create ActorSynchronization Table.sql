CREATE TABLE [dbo].[ActorSynchronization]
(
    [Id]             [int] NOT NULL,
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [ActorId]        [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_ActorSynchronization] PRIMARY KEY CLUSTERED ([Id])
)
GO