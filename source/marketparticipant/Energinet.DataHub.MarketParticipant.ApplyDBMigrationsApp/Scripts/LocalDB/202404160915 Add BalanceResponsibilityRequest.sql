CREATE TABLE [dbo].[BalanceResponsibilityRequest]
(
    [Id]                      [int] IDENTITY(1,1) PRIMARY KEY,
    [EnergySupplier]          [nvarchar](50)      NOT NULL,
    [BalanceResponsibleParty] [nvarchar](50)      NOT NULL,
    [GridAreaCode]            [nvarchar](4)       NOT NULL,
    [MeteringPointType]       [int]               NOT NULL,
    [ValidFrom]               [datetimeoffset](7) NOT NULL,
    [ValidTo]                 [datetimeoffset](7) NULL,
) ON [PRIMARY]
