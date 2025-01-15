CREATE TABLE [electricitymarket].[EnergySupplyPeriod]
(
    [Id]                   bigint IDENTITY(1,1) NOT NULL,
    [CommercialRelationId] bigint NOT NULL,
    [ValidFrom]            datetimeoffset NOT NULL,
    [ValidTo]              datetimeoffset NOT NULL,
    [RetiredById]          bigint NULL,
    [RetiredAt]            datetimeoffset NULL,
    [CreatedAt]            datetimeoffset NOT NULL

    CONSTRAINT PK_EnergySupplyPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_EnergySupplyPeriod_EnergySupplyPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[EnergySupplyPeriod]([ID]),
    CONSTRAINT FK_EnergySupplyPeriod_CommercialRelation FOREIGN KEY (CommercialRelationId) REFERENCES [electricitymarket].[CommercialRelation]([ID])
)
