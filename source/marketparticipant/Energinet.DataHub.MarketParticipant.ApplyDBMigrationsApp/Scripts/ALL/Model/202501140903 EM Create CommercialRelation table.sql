CREATE TABLE [electricitymarket].[CommercialRelation]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [MeteringPointId]    bigint NOT NULL,
    [EnergySupplier]     varchar(16) NOT NULL,
    [StartDate]          datetimeoffset NOT NULL,
    [EndDate]            datetimeoffset NOT NULL,
    [ModifiedAt]         datetimeoffset NOT NULL

    CONSTRAINT PK_CommercialRelation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CommercialRelation_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID])
)
