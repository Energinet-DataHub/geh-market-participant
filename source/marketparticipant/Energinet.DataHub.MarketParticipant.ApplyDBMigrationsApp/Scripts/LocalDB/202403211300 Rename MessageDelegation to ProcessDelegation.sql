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

ALTER TABLE [dbo].[DelegationPeriod] ADD

    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_DelegationPeriod_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_DelegationPeriod_PeriodEnd   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
    
    Version             int              NOT NULL DEFAULT 0,
    ChangedByIdentityId uniqueidentifier NOT NULL
GO

ALTER TABLE [dbo].[DelegationPeriod]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[DelegationPeriodHistory]));
GO

ALTER TABLE [dbo].[DelegationPeriod]
    ADD CONSTRAINT CHK_DelegationPeriod_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000')
GO
