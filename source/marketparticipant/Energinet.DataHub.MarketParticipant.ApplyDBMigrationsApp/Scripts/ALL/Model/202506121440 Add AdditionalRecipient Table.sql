CREATE TABLE [dbo].[AdditionalRecipient]
(
    [Id]      [int]              IDENTITY(1,1) NOT NULL,
    [ActorId] [uniqueidentifier] NOT NULL,
    
    CONSTRAINT [PK_AdditionalRecipient] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_AdditionalRecipient_ActorId] UNIQUE ([ActorId]),
    CONSTRAINT [FK_AdditionalRecipient_Actor] FOREIGN KEY ([ActorId]) REFERENCES [dbo].[Actor]([Id])
);

CREATE TABLE [dbo].[AdditionalRecipientOfMeteringPoint]
(
    [Id]                          [int]      IDENTITY(1,1) NOT NULL,
    [AdditionalRecipientId]       [int]      NOT NULL,
    [MeteringPointIdentification] [char](18) NOT NULL,

    CONSTRAINT [PK_AdditionalRecipientOfMeteringPoint] PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [FK_AdditionalRecipientOfMeteringPoint_AdditionalRecipient] FOREIGN KEY ([AdditionalRecipientId]) REFERENCES [dbo].[AdditionalRecipient]([Id])
);

CREATE UNIQUE CLUSTERED INDEX IX_AdditionalRecipientOfMeteringPoint_MeteringPointIdentification
    ON [dbo].[AdditionalRecipientOfMeteringPoint] ([MeteringPointIdentification], [AdditionalRecipientId])

CREATE INDEX IX_AdditionalRecipientOfMeteringPoint_AdditionalRecipientId
    ON [dbo].[AdditionalRecipientOfMeteringPoint] ([AdditionalRecipientId])

ALTER TABLE [dbo].[AdditionalRecipientOfMeteringPoint] ADD

    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_AdditionalRecipientOfMeteringPoint_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_AdditionalRecipientOfMeteringPoint_PeriodEnd   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
    
    Version             int              NOT NULL DEFAULT 0,
    ChangedByIdentityId uniqueidentifier NOT NULL,
    DeletedByIdentityId uniqueidentifier NULL
GO

ALTER TABLE [dbo].[AdditionalRecipientOfMeteringPoint]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[AdditionalRecipientOfMeteringPointHistory]));
GO

ALTER TABLE [dbo].[AdditionalRecipientOfMeteringPoint]
    ADD CONSTRAINT CHK_AdditionalRecipientOfMeteringPoint_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000'),
        CONSTRAINT CHK_AdditionalRecipientOfMeteringPoint_DeletedByIdentityId_NotEmpty CHECK (DeletedByIdentityId <> '00000000-0000-0000-0000-000000000000');
GO
