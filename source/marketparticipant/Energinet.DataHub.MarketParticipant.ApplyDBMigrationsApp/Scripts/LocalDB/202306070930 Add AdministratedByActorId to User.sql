ALTER TABLE [dbo].[User]
ADD [AdministratedByActorId] [uniqueidentifier] NOT NULL
CONSTRAINT DF_AdministratedByActorId DEFAULT('00000000-0000-0000-0000-000000000000')
GO

DECLARE @u_id uniqueidentifier, @a_id uniqueidentifier;

DECLARE user_cursor CURSOR FOR
    SELECT u.Id, r.ActordId
    FROM [dbo].[User] AS u
        OUTER APPLY (
            SELECT TOP 1 r.ActorId
            FROM [dbo].[UserRoleAssignment] r
            WHERE r.userid = u.id
        ) AS r
    WHERE u.AdministratedByActorId = '00000000-0000-0000-0000-000000000000';

OPEN user_cursor;
FETCH NEXT FROM user_cursor INTO @u_id, @a_id;

WHILE @@fetch_status = 0
BEGIN
    -- update user
    UPDATE [dbo].[User]
    SET AdministratedByActorId = @a_id
    WHERE id = @u_id AND @a_id IS NOT NULL;

    -- fetch next
    FETCH NEXT FROM user_cursor INTO @u_id, @a_id;
END

CLOSE user_cursor;
DEALLOCATE user_cursor;
GO

ALTER TABLE [dbo].[User]
ADD FOREIGN KEY (AdministratedByActorId) REFERENCES dbo.ActorInfoNew(Id)
GO

ALTER TABLE [dbo].[User]
DROP CONSTRAINT DF_AdministratedByActorId
GO
