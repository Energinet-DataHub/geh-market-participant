CREATE TABLE [dbo].[ActorDelegation]
(
    [Id] [uniqueidentifier] NOT NULL,
    [DelegatedBy] [uniqueidentifier] NOT NULL,
    [DelegatedTo] [uniqueidentifier] NOT NULL,
    [MessageType] [int] NOT NULL,
    [Starts] [datetimeoffset](7) NOT NULL,
    [Expires] [datetimeoffset](7) NULL,
    CONSTRAINT [PK_ActorContact] PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
