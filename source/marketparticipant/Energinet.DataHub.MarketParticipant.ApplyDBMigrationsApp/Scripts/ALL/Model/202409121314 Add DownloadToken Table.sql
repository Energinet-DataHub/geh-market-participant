CREATE TABLE [dbo].[DownloadTokens]
(
    [Token] [uniqueidentifier] NOT NULL,
    [Used] [BIT] NOT NULL,
    [Created] [datetimeoffset](7) NOT NULL,
    [Authorization] [nvarchar](max) NOT NULL,

    CONSTRAINT [PK_DownloadTokens] PRIMARY KEY CLUSTERED ([Token] ASC),
) ON [PRIMARY]


