SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DomainEvent]
(
    [Id]            [uniqueidentifier] NOT NULL,
    [EntityId]      [uniqueidentifier] NOT NULL,
    [EntityType]    [nvarchar](32)     NOT NULL,
    [Timestamp]     [datetime]         NOT NULL,
    [Event]         [nvarchar](max)    NOT NULL
        CONSTRAINT [PK_DomainEvent_Id] PRIMARY KEY CLUSTERED
            (
             [Id] ASC
                ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
