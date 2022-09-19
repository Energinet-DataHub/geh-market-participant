DECLARE @org_id uniqueidentifier, @act_id uniqueidentifier;

-- cursor for all actors with status 'Deleted'
DECLARE actor_cursor CURSOR  
    FOR 
		SELECT o.Id as OrganizationId, a.Id as ActorId
		FROM [dbo].[ActorInfoNew] as a 
		JOIN [dbo].[OrganizationInfo] o on a.OrganizationId = o.Id
		WHERE a.Status = 5

OPEN actor_cursor

FETCH NEXT FROM actor_cursor
INTO @org_id, @act_id

WHILE @@FETCH_STATUS = 0
BEGIN
    -- set status of current actor to 'Inactive'
	UPDATE [dbo].[ActorInfoNew]
	SET [Status] = 3
	WHERE Id = @act_id;

    -- fetch newest 'ActorUpdatedIntegrationEvent' for the current actor
    DECLARE @updatedEventData nvarchar(MAX) = (SELECT TOP 1 [Event] FROM [dbo].[DomainEvent] WHERE EntityId = @act_id AND EventTypeName = 'ActorUpdatedIntegrationEvent' ORDER BY [Timestamp] DESC)

    -- replace properties; status, id and eventCreated
    SET @updatedEventData = (SELECT STUFF(@updatedEventData, PATINDEX('%status%', @updatedEventData), 9, 'status":3'))
    SET @updatedEventData = (SELECT STUFF(@updatedEventData, PATINDEX('%],"id"%', @updatedEventData), 45, '],"id":"' + LOWER(CONVERT(nvarchar(36), NEWID())) + '"'))
    SET @updatedEventData = (SELECT STUFF(@updatedEventData, PATINDEX('%eventCreated%', @updatedEventData), 43, 'eventCreated":"' + FORMAT(GETUTCDATE(), 'yyyy-MM-ddTHH:mm:ss.999999Z') + '"'))
	
	-- insert 'ActorUpdatedIntegrationEvent' for current actor
	INSERT INTO [dbo].[DomainEvent](EntityId, EntityType, IsSent, [Timestamp], [Event], EventTypeName)
	VALUES(@act_id, 'Actor', 0, SYSDATETIMEOFFSET(), @updatedEventData, 'ActorUpdatedIntegrationEvent')

	-- insert 'ActorStatusChangedIntegrationEvent' for current actor
	DECLARE @statusChangedEventData nvarchar(MAX) = 
		'{' +
            '"organizationId":{"value":"' + LOWER(CONVERT(nvarchar(36), @org_id)) + '"},' +
            '"actorId":"' + LOWER(CONVERT(nvarchar(36), @act_id)) + '",' +
            '"status":3,' +
            '"id":"' + LOWER(CONVERT(nvarchar(36), NEWID())) + '",' +
            '"eventCreated":"' + FORMAT(GETUTCDATE(), 'yyyy-MM-ddTHH:mm:ss.999999Z') + '"' +
        '}'
	
	INSERT INTO [dbo].[DomainEvent](EntityId, EntityType, IsSent, [Timestamp], [Event], EventTypeName)
	VALUES(@act_id, 'Actor', 0, SYSDATETIMEOFFSET(), @statusChangedEventData, 'ActorStatusChangedIntegrationEvent')

	FETCH NEXT FROM actor_cursor
	INTO @org_id, @act_id
END

CLOSE actor_cursor
DEALLOCATE actor_cursor
