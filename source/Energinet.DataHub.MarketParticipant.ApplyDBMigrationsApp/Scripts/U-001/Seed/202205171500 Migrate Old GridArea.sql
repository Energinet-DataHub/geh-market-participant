INSERT INTO [dbo].[GridAreaNew]
SELECT [Id], [Code], [Name], CASE WHEN [PriceAreaCode] = 'DK1' THEN 1 ELSE 2 END as PriceAreaCode
FROM [dbo].[GridAreaInfo]

INSERT INTO [dbo].[GridAreaLinkNew]
SELECT [GridLinkId], [GridAreaId]
FROM [dbo].[GridAreaLinkInfo]