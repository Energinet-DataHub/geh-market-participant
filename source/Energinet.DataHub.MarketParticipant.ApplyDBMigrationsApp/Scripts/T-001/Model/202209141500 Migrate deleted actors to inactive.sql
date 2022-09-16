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

	-- insert 'ActorStatusChangedIntegrationEvent' for current actor
	DECLARE @eventData nvarchar(MAX) = 
		'{' +
            '"organizationId":{"value":"' + LOWER(CONVERT(nvarchar(36), @org_id)) + '"},' +
            '"actorId":"' + LOWER(CONVERT(nvarchar(36), @act_id)) + '",' +
            '"status":3,' +
            '"id":"' + LOWER(CONVERT(nvarchar(36), NEWID())) + '",' +
            '"eventCreated":"' + FORMAT(GETUTCDATE(), 'yyyy-MM-ddTHH:mm:ss.999999Z') + '"' +
        '}'
	
	INSERT INTO [dbo].[DomainEvent](EntityId, EntityType, IsSent, [Timestamp], [Event], EventTypeName)
	VALUES(@act_id, 'Actor', 0, SYSDATETIMEOFFSET(), @eventData, 'ActorStatusChangedIntegrationEvent')

	FETCH NEXT FROM actor_cursor
	INTO @org_id, @act_id
END

CLOSE actor_cursor
DEALLOCATE actor_cursor
