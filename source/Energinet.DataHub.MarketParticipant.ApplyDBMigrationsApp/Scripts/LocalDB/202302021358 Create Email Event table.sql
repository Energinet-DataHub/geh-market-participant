SET ANSI_NULLS ON
GO
    
SET QUOTED_IDENTIFIER ON
GO
    
CREATE TABLE [dbo].[EmailEvent]
(
    [Id]             [int] IDENTITY(1, 1)   NOT NULL,
    [UserId]         [uniqueidentifier]     NOT NULL,
    [ActorId]        [uniqueidentifier]     NOT NULL,
    [Created]        [datetimeoffset]       NOT NULL,
    [Sent]           [datetimeoffset]       NULL,
    [EmailEventType] [int]                  NOT NULL
    
    CONSTRAINT [PK_EmailEvent_Id] PRIMARY KEY CLUSTERED([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_EmailEvent_UserId FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_EmailEvent_ActorId FOREIGN KEY ([ActorId]) REFERENCES [dbo].[ActorInfoNew]([Id])
) ON [PRIMARY]
GO
