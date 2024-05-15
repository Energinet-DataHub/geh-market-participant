CREATE TABLE [dbo].[BalanceResponsibilityAgreement]
(
    [Id]                        [uniqueidentifier]  NOT NULL,
    [EnergySupplierId]          [uniqueidentifier]  NOT NULL,
    [BalanceResponsiblePartyId] [uniqueidentifier]  NOT NULL,
    [GridAreaId]                [uniqueidentifier]  NOT NULL,
    [MeteringPointType]         [int]               NOT NULL,
    [ValidFrom]                 [datetimeoffset](7) NOT NULL,
    [ValidTo]                   [datetimeoffset](7) NULL,

    CONSTRAINT [PK_BalanceResponsibilityAgreement] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_BalanceResponsibilityAgreement_EnergySupplierId_Actor] FOREIGN KEY([EnergySupplierId]) REFERENCES [dbo].[Actor] ([Id]),
    CONSTRAINT [FK_BalanceResponsibilityAgreement_BalanceResponsiblePartyId_Actor] FOREIGN KEY([BalanceResponsiblePartyId]) REFERENCES [dbo].[Actor] ([Id]),
    CONSTRAINT [FK_BalanceResponsibilityAgreement_GridAreaId_GridArea] FOREIGN KEY([GridAreaId]) REFERENCES [dbo].[GridArea] ([Id]),
) ON [PRIMARY]
