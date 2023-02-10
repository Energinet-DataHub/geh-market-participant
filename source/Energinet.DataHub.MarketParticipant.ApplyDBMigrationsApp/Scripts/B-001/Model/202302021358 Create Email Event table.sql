SET ANSI_NULLS ON
GO
    
SET QUOTED_IDENTIFIER ON
GO
    
CREATE TABLE [dbo].[EmailEvent]
(
    [Id]             [int] IDENTITY(1, 1)   NOT NULL,
    [Email]          [nvarchar](255)        NOT NULL DEFAULT(''),
    [Created]        [datetimeoffset]       NOT NULL,
    [Sent]           [datetimeoffset]       NULL,
    [EmailEventType] [int]                  NOT NULL
    
    CONSTRAINT [PK_EmailEvent_Id] PRIMARY KEY CLUSTERED([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
