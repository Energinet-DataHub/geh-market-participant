CREATE VIEW [electricitymarket].vw_MeteringPointChanges AS
SELECT
    mp.Identification,
    mpp.ValidFrom,
    mpp.ValidTo,
    mpp.GridAreaCode,
    GridAccessProvider = mpp.OwnedBy,
    mpp.ConnectionState,
    mpp.Type,
    mpp.SubType,
    mpp.Resolution,
    mpp.Unit,
    mpp.ProductId,
    mpp.ParentIdentification
FROM [electricitymarket].[MeteringPoint] mp
JOIN [electricitymarket].[MeteringPointPeriod] mpp ON mp.Id = mpp.MeteringPointId
WHERE mpp.RetiredById IS NULL
