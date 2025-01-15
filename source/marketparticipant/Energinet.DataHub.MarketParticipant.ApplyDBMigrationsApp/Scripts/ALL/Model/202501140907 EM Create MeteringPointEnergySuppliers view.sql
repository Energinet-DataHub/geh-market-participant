CREATE VIEW [electricitymarket].vw_MeteringPointEnergySuppliers AS
SELECT
    mp.Identification,
    cr.EnergySupplier,
    cr.StartDate,
    cr.EndDate
FROM [electricitymarket].[MeteringPoint] mp
JOIN [electricitymarket].[CommercialRelation] cr ON mp.Id = cr.MeteringPointId
WHERE cr.StartDate < cr.EndDate
