CREATE TABLE [dbo].[ActorConsolidation](
    [Id]                    [uniqueidentifier]  NOT NULL,
    [ActorFromId]           [uniqueidentifier]  NOT NULL,
    [ActorToId]             [uniqueidentifier]  NOT NULL,
    [GridAreaToMergeToId]   [uniqueidentifier]  NOT NULL,
    [ScheduledAt]           [datetimeoffset](7) NOT NULL,
    [Status]                [int]               NOT NULL,

    CONSTRAINT [PK_ActorConsolidation] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_ActorConsolidation_ActorFromId_Actor FOREIGN KEY ([ActorFromId]) REFERENCES [dbo].[Actor]([Id]),
    CONSTRAINT FK_ActorConsolidation_ActorToId_Actor FOREIGN KEY ([ActorToId]) REFERENCES [dbo].[Actor]([Id]),
    CONSTRAINT FK_ActorConsolidation_GridAreaToMergeToId_GridArea FOREIGN KEY ([GridAreaToMergeToId]) REFERENCES [dbo].[GridArea]([Id]),
    ) ON [PRIMARY]
    GO