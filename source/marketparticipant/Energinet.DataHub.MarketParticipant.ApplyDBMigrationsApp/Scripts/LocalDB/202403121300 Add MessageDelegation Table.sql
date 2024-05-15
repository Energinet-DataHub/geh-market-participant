CREATE TABLE [dbo].[MessageDelegation]
(
    [Id]                 [uniqueidentifier] NOT NULL,
    [DelegatedByActorId] [uniqueidentifier] NOT NULL,
    [MessageType]        [int] NOT NULL,
    [ConcurrencyToken]   [uniqueidentifier] NOT NULL,

    CONSTRAINT [PK_MessageDelegation] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MessageDelegation_DelegatedByActorId_Actor] FOREIGN KEY([DelegatedByActorId]) REFERENCES [dbo].[Actor] ([Id]),
    CONSTRAINT [UQ_MessageDelegation_DelegatedByActorId_MessageType] UNIQUE NONCLUSTERED ([DelegatedByActorId], [MessageType])
) ON [PRIMARY]

CREATE TABLE [dbo].[DelegationPeriod]
(
    [Id]                  [uniqueidentifier] NOT NULL,
    [MessageDelegationId] [uniqueidentifier] NOT NULL,
    [DelegatedToActorId]  [uniqueidentifier] NOT NULL,
    [GridAreaId]          [uniqueidentifier] NOT NULL,
    [StartsAt]            [datetimeoffset](7) NOT NULL,
    [StopsAt]             [datetimeoffset](7) NULL,

    CONSTRAINT [PK_DelegationPeriod] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DelegationPeriod_GridAreaId_GridArea] FOREIGN KEY([GridAreaId]) REFERENCES [dbo].[GridArea] ([Id]),
    CONSTRAINT [FK_DelegationPeriod_DelegatedToActorId_Actor] FOREIGN KEY([DelegatedToActorId]) REFERENCES [dbo].[Actor] ([Id]),
    CONSTRAINT [FK_DelegationPeriod_MessageDelegationId_MessageDelegation] FOREIGN KEY([MessageDelegationId]) REFERENCES [dbo].[MessageDelegation] ([Id])
) ON [PRIMARY]
