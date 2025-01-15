CREATE TABLE [electricitymarket].[MeteringPointPeriod]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [MeteringPointId]            bigint NOT NULL,
    [ValidFrom]                  datetimeoffset NOT NULL,
    [ValidTo]                    datetimeoffset NOT NULL,
    [RetiredById]                bigint NULL,
    [RetiredAt]                  datetimeoffset NULL,
    [CreatedAt]                  datetimeoffset NOT NULL,
    [GridAreaCode]               char(3) NOT NULL,
    [OwnedBy]                    varchar(16) NOT NULL,
    [ConnectionState]            varchar(64) NOT NULL,
    [Type]                       varchar(64) NOT NULL,
    [SubType]                    varchar(64) NOT NULL,
    [Resolution]                 varchar(6) NOT NULL,
    [Unit]                       varchar(64) NOT NULL,
    [ProductId]                  varchar(64) NOT NULL,
    [SettlementGroup]            int NULL,
    [ScheduledMeterReadingMonth] int NOT NULL,
    [ParentIdentification]       bigint NULL

    CONSTRAINT PK_MeteringPointPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPointPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[MeteringPointPeriod]([ID]),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID])
)
