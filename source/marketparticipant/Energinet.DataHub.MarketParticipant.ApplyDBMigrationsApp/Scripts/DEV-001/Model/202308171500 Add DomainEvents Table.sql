IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DomainEvent]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[DomainEvent]
    (
        [Id]            [int] IDENTITY(1, 1) NOT NULL,
        [EntityId]      [uniqueidentifier]   NOT NULL,
        [EntityType]    [nvarchar](32)       NOT NULL,
        [IsSent]        [bit]                NOT NULL,
        [Timestamp]     [datetimeoffset]     NOT NULL,
        [Event]         [nvarchar](max)      NOT NULL,
        [EventTypeName] [nvarchar](128)      NOT NULL
        CONSTRAINT [PK_DomainEvent_Id] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY]
END
