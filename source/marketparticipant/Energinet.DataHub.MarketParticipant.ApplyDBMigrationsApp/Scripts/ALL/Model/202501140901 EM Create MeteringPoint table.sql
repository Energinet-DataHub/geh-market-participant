CREATE TABLE [electricitymarket].[MeteringPoint]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [Identification]     char(18) NOT NULL

    CONSTRAINT PK_MeteringPoint PRIMARY KEY CLUSTERED (Id)
)
