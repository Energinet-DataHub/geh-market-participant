DECLARE @ActorId AS UNIQUEIDENTIFIER
SET @ActorId = 'f02c526c-9224-4bde-9d51-08dc32d95938'
    
DECLARE @GridAreaId AS UNIQUEIDENTIFIER
SET @GridAreaId = (SELECT [Id] FROM [dbo].[GridArea] WHERE [Code] = '996')
    
DECLARE @MarketRoleId AS UNIQUEIDENTIFIER
SET @MarketRoleId = (SELECT [Id] FROM [dbo].[MarketRole] WHERE [ActorId] = @ActorId AND [Function] = 14)

INSERT INTO [dbo].[MarketRoleGridArea]
    ([Id], [MarketRoleId], [GridAreaId])
VALUES
    (NEWID(), @MarketRoleId, @GridAreaId)

INSERT INTO [dbo].[DomainEvent]
    ([EntityId], [EntityType], [IsSent], [Timestamp], [Event], [EventTypeName])
VALUES
    (@ActorId,
    'Actor',
    0,
    GETDATE(),
    '{"actorNumber":{"$type":"gln","type":1,"value":"7331507000006"},"actorRole":14,"gridAreaId":{"value":"' + CONVERT(NVARCHAR(36), @GridAreaId) + '"},"validFrom":"2024-01-01T23:00:00Z","eventId":"D2F70216-F919-4E1A-8C37-B2653211F778"}',
    'GridAreaOwnershipAssigned')
