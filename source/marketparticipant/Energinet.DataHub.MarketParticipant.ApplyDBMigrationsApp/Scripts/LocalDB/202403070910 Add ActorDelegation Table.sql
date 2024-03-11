CREATE TABLE [dbo].[ActorDelegation]
(
    [Id] [uniqueidentifier] NOT NULL,
    [DelegatedByActorId] [uniqueidentifier] NOT NULL,
    [DelegatedToActorId] [uniqueidentifier] NOT NULL,
    [GridAreaId] [uniqueidentifier] NOT NULL,
    [MessageType] [int] NOT NULL,
    [StartsAt] [datetimeoffset](7) NOT NULL,
    [ExpiresAt] [datetimeoffset](7) NULL,
    CONSTRAINT [PK_ActorDelegation] PRIMARY KEY CLUSTERED
(
[Id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [FK_ActorDelegationGridAreaId_GridArea] FOREIGN KEY([GridAreaId]) REFERENCES [dbo].[GridArea] ([Id]),
    CONSTRAINT [FK_ActorDelegationDelegatedByActorId_Actor] FOREIGN KEY([DelegatedByActorId]) REFERENCES [dbo].[Actor] ([Id]),
    CONSTRAINT [FK_ActorDelegationDelegatedToActorIdActor] FOREIGN KEY([DelegatedToActorId]) REFERENCES [dbo].[Actor] ([Id])
    ) ON [PRIMARY]