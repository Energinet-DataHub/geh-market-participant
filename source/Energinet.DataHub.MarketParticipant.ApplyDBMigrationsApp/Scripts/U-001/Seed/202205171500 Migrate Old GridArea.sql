INSERT INTO [dbo].[GridAreaNew]
SELECT [Id], [Code], [Name], CASE WHEN [PriceAreaCode] = 'DK1' THEN 1 ELSE 2 END as PriceAreaCode
FROM [dbo].[GridAreaInfo]

INSERT INTO [dbo].[GridAreaLinkNew]
SELECT [GridLinkId], [GridAreaId]
FROM [dbo].[GridAreaLinkInfo]

UPDATE [dbo].[ActorInfoNew]
SET [GridAreaId] = [dbo].[GridAreaInfo].[Id]
FROM [dbo].[ActorInfoNew] INNER JOIN [dbo].[GridAreaInfo]
	ON [dbo].[ActorInfoNew].[ActorId] = [dbo].[GridAreaInfo].[ActorId]