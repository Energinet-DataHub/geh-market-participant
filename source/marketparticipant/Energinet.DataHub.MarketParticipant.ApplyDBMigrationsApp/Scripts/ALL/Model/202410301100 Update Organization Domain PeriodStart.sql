UPDATE od
SET od.PeriodStart = o.PeriodStart
FROM OrganizationDomain od JOIN Organization o ON od.OrganizationId = o.Id
WHERE od.OrganizationId NOT IN (SELECT Id FROM OrganizationHistory)
GO

UPDATE od
SET od.PeriodStart = oh.ActualPeriodStart
FROM OrganizationDomain od JOIN (SELECT Id, MIN(PeriodStart) 'ActualPeriodStart' FROM OrganizationHistory GROUP BY Id) AS oh ON od.OrganizationId = oh.id
WHERE od.OrganizationId IN (SELECT Id FROM OrganizationHistory)
GO