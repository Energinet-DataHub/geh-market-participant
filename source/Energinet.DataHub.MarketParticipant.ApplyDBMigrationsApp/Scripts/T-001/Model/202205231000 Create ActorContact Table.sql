CREATE TABLE [dbo].[ActorContact]
(
    [Id]                [uniqueidentifier] NOT NULL,
    [ActorId]           [uniqueidentifier] NOT NULL,
    [Category]          [int]              NOT NULL,
    [Name]              [nvarchar](250)    NOT NULL,
    [Email]             [nvarchar](250)    NOT NULL,
    [Phone]             [nvarchar](250)    NOT NULL,

    CONSTRAINT PK_Contact PRIMARY KEY ([Id]),
    CONSTRAINT FK_Actor FOREIGN KEY ([ActorId]) REFERENCES [dbo].[ActorInfoNew]([Id]),
)
GO