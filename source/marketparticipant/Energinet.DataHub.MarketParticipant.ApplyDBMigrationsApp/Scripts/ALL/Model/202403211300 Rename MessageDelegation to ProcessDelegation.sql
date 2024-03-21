ALTER TABLE [dbo].[DelegationPeriod]
	SET (SYSTEM_VERSIONING = OFF)
GO

DROP TABLE [dbo].[DelegationPeriodHistory]
DROP TABLE [dbo].[DelegationPeriod]
DROP TABLE [dbo].[MessageDelegation]
GO

CREATE TABLE [dbo].[ProcessDelegation]
(
    [Id]                 [uniqueidentifier] NOT NULL,
    [DelegatedByActorId] [uniqueidentifier] NOT NULL,
    [DelegatedProcess]   [int]              NOT NULL,
    [ConcurrencyToken]   [uniqueidentifier] NOT NULL,

    CONSTRAINT [PK_ProcessDelegation] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProcessDelegation_DelegatedByActorId_Actor] FOREIGN KEY([DelegatedByActorId]) REFERENCES [dbo].[Actor] ([Id]),
    CONSTRAINT [UQ_ProcessDelegation_DelegatedByActorId_DelegatedProcess] UNIQUE NONCLUSTERED ([DelegatedByActorId], [DelegatedProcess])
) ON [PRIMARY]

CREATE TABLE [dbo].[DelegationPeriod]
(
    [Id]                  [uniqueidentifier] NOT NULL,
    [ProcessDelegationId] [uniqueidentifier] NOT NULL,
    [DelegatedToActorId]  [uniqueidentifier] NOT NULL,
    [GridAreaId]          [uniqueidentifier] NOT NULL,
    [StartsAt]            [datetimeoffset](7) NOT NULL,
    [StopsAt]             [datetimeoffset](7) NULL,

    CONSTRAINT [PK_DelegationPeriod] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DelegationPeriod_GridAreaId_GridArea] FOREIGN KEY([GridAreaId]) REFERENCES [dbo].[GridArea] ([Id]),
    CONSTRAINT [FK_DelegationPeriod_DelegatedToActorId_Actor] FOREIGN KEY([DelegatedToActorId]) REFERENCES [dbo].[Actor] ([Id]),
    CONSTRAINT [FK_DelegationPeriod_ProcessDelegationId_ProcessDelegation] FOREIGN KEY([ProcessDelegationId]) REFERENCES [dbo].[ProcessDelegation] ([Id])
) ON [PRIMARY]
