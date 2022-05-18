INSERT INTO [dbo].[GridAreaActorInfoLink]
SELECT [GridAreaId], [ActorInfoId]
FROM [dbo].[ActorInfoNew] INNER JOIN [dbo].[GridAreaInfo]
	ON [dbo].[ActorInfoNew].[ActorId] = [dbo].[GridAreaInfo].[ActorId]