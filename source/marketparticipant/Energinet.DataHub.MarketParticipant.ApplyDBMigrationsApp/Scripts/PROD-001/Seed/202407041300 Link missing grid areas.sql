CREATE TABLE #ActorIdToGridAreaCodeLookup
(
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    ActorNumber  NVARCHAR(13),
    GridAreaCode NVARCHAR(3),
);

INSERT INTO #ActorIdToGridAreaCodeLookup (ActorNumber, GridAreaCode)
VALUES ('5790000705689', '755'),
       ('5790000610976', '512'),
       ('5790001089030', '152'),
       ('5790001089030', '398');

DECLARE
    @Id INT = 1,
    @MaxId INT = (SELECT MAX(Id) FROM #ActorIdToGridAreaCodeLookup);

DECLARE
    @ActorId UNIQUEIDENTIFIER,
    @ActorNumber NVARCHAR(13),
    @GridAreaId UNIQUEIDENTIFIER,
    @GridAreaCode NVARCHAR(3),
    @MarketRoleId UNIQUEIDENTIFIER;
        
WHILE @Id <= @MaxId
BEGIN

    SELECT @ActorNumber = ActorNumber, @GridAreaCode = GridAreaCode
    FROM #ActorIdToGridAreaCodeLookup
    WHERE Id = @Id;

    SET @ActorId = (SELECT [Id] FROM [dbo].[Actor] WHERE [ActorNumber] = @ActorNumber)
    SET @GridAreaId = (SELECT [Id] FROM [dbo].[GridArea] WHERE [Code] = @GridAreaCode)
    SET @MarketRoleId = (SELECT [Id] FROM [dbo].[MarketRole] WHERE [ActorId] = @ActorId AND [Function] = 14)

    INSERT INTO [dbo].[MarketRoleGridArea] ([Id], [MarketRoleId], [GridAreaId])
    VALUES (NEWID(), @MarketRoleId, @GridAreaId)

    INSERT INTO [dbo].[DomainEvent] ([EntityId], [EntityType], [IsSent], [Timestamp], [Event], [EventTypeName])
    VALUES (@ActorId, 'Actor', 0, GETUTCDATE(), '{"actorNumber":{"$type":"gln","type":1,"value":"' + @ActorNumber + '"},"actorRole":14,"gridAreaId":{"value":"' + CONVERT(NVARCHAR(36), @GridAreaId) + '"},"validFrom":"2021-01-01T23:00:00Z","eventId":"' + CONVERT(NVARCHAR(36), NEWID()) + '"}', 'GridAreaOwnershipAssigned')

    SET @Id = @Id + 1;

END

DROP TABLE #ActorIdToGridAreaCodeLookup;