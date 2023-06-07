ALTER TABLE [dbo].[User]
    ADD [AdministratedByActorId] [uniqueidentifier] NOT NULL DEFAULT('00000000-0000-0000-0000-000000000000')
GO

declare @u_id uniqueidentifier, @a_id uniqueidentifier;

declare user_cursor cursor for
    select u.id, r.actorid
    from [dbo].[user] as u
        outer apply (
            select top 1 r.actorid
            from [dbo].[userroleassignment] r
            where r.userid = u.id
        ) as r
    where u.administratedbyactorid = '00000000-0000-0000-0000-000000000000';

open user_cursor;

fetch next from user_cursor into @u_id, @a_id;

while @@fetch_status = 0
begin
    -- update user
    update [dbo].[user]
    set administratedbyactorid = @a_id
    where id = @u_id and @a_id is not null;

    -- fetch next
    fetch next from user_cursor into @u_id, @a_id;
end

close user_cursor;
deallocate user_cursor;

GO

ALTER TABLE [dbo].[User]
ADD FOREIGN KEY (AdministratedByActorId) REFERENCES dbo.ActorInfoNew(Id)
GO
