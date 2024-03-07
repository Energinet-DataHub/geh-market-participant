CREATE TABLE [dbo].[ActorDelegationGridArea](
    [Id] [uniqueidentifier] NOT NULL,
    [ActorDelegationGridAreaId] [uniqueidentifier] NOT NULL,
    [GridAreaId] [uniqueidentifier] NOT NULL,
     CONSTRAINT [PK_ActorDelegationGridArea] PRIMARY KEY CLUSTERED
    (
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]