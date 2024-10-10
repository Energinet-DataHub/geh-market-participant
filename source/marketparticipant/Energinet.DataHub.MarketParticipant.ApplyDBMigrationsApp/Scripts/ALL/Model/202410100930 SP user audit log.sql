CREATE PROCEDURE GetUserAuditsForPeriod
    @FromDateInclusive DATETIME,
    @ToDateExclusive DATETIME
AS
BEGIN
    select * from
    (
        (
            select
                executing_user.Email as EXECUTING_USER_MAIL,
                affected_user.Email as AFFECTED_USER_MAIL,
                concat('"', affected_user_actor.ActorNumber, '"') as AFFECTED_ACTOR,
                case
                    when user_identity_audit_log.Field = 1 then 'UPDATED_FIRST_NAME'
                    when user_identity_audit_log.Field = 2 then 'UPDATED_LAST_NAME'
                    when user_identity_audit_log.Field = 3 then 'UPDATED_PHONE_NO'
                    when user_identity_audit_log.Field = 4 then 'UPDATED_STATUS'
                    when user_identity_audit_log.Field = 5 then 'UPDATED_USER_LOGIN_FEDERATED_REQUESTED'
                    when user_identity_audit_log.Field = 6 then 'UPDATED_USER_LOGIN_FEDERATED'
                    else CAST(user_identity_audit_log.Field as varchar)
                  end as [ACTION],
                  user_identity_audit_log.NewValue as VALUE_TO,
                  user_identity_audit_log.Timestamp as OCCURRED_ON,
                  FORMAT(user_identity_audit_log.Timestamp at TIME ZONE 'UTC' at TIME ZONE 'Central European Standard Time', 'yyyy-MM-dd HH:mm') as OCCURRED_ON_CET
            from [dbo].[UserIdentityAuditLogEntry] user_identity_audit_log
                join [dbo].[User] executing_user on user_identity_audit_log.ChangedByUserId = executing_user.Id
                join [dbo].[User] affected_user on user_identity_audit_log.UserId = affected_user.Id
                join [dbo].[Actor] affected_user_actor on affected_user.AdministratedByActorId = affected_user_actor.Id
        )
        union
        (
            select
                executing_user.Email as EXECUTING_USER_MAIL,
                affected_user.Email as AFFECTED_USER_MAIL,
                concat('"', affected_user_actor.ActorNumber, '"') as AFFECTED_ACTOR,
                case
                    when user_role_assignment.DeletedByIdentityId is null then 'ASSIGNED_USER_ROLE'
                    when user_role_assignment.DeletedByIdentityId is not null then 'REMOVED_USER_ROLE'
                end as [ACTION],
                user_role.Name as VALUE_TO,
	            user_role_assignment.PeriodStart as OCCURRED_ON,
                FORMAT(user_role_assignment.PeriodStart at TIME ZONE 'UTC' at TIME ZONE 'Central European Standard Time', 'yyyy-MM-dd HH:mm') as OCCURRED_ON_CET
            from [dbo].[UserRoleAssignment] user_role_assignment
                join [dbo].[User] executing_user on
                    (user_role_assignment.ChangedByIdentityId = executing_user.Id and user_role_assignment.DeletedByIdentityId is null) or
                    (user_role_assignment.DeletedByIdentityId = executing_user.Id)
                join [dbo].[User] affected_user on user_role_assignment.UserId = affected_user.Id
                join [dbo].[Actor] affected_user_actor on user_role_assignment.ActorId = affected_user_actor.Id
                join [dbo].[UserRole] user_role on user_role.Id = user_role_assignment.UserRoleid
        )
        union
        (
            select
                executing_user.Email as EXECUTING_USER_MAIL,
                affected_user.Email as AFFECTED_USER_MAIL,
                concat('"', affected_user_actor.ActorNumber, '"') as AFFECTED_ACTOR,
                case
                    when user_role_assignment_history.DeletedByIdentityId is null then 'ASSIGNED_USER_ROLE'
                    when user_role_assignment_history.DeletedByIdentityId is not null then 'REMOVED_USER_ROLE'
                end as [ACTION],
                user_role.Name as VALUE_TO,
	            user_role_assignment_history.PeriodStart as OCCURRED_ON,
                FORMAT(user_role_assignment_history.PeriodStart at TIME ZONE 'UTC' at TIME ZONE 'Central European Standard Time', 'yyyy-MM-dd HH:mm') as OCCURRED_ON_CET
            from [dbo].[UserRoleAssignmentHistory] user_role_assignment_history
                join [dbo].[User] executing_user on
                    (user_role_assignment_history.ChangedByIdentityId = executing_user.Id and user_role_assignment_history.DeletedByIdentityId is null) or
                    (user_role_assignment_history.DeletedByIdentityId = executing_user.Id)
                join [dbo].[User] affected_user on user_role_assignment_history.UserId = affected_user.Id
                join [dbo].[Actor] affected_user_actor on user_role_assignment_history.ActorId = affected_user_actor.Id
                join [dbo].[UserRole] user_role on user_role.Id = user_role_assignment_history.UserRoleid
        )
    ) as audit_log
    where @FromDateInclusive <= OCCURRED_ON and OCCURRED_ON < @ToDateExclusive
    order by OCCURRED_ON
END;
