CREATE PROCEDURE GetUserRoleAuditsForPeriod
    @FromDateInclusive DATETIME,
    @ToDateExclusive DATETIME
AS
BEGIN
    select MARKET_ROLE, USER_ROLE, EXECUTING_USER_MAIL, [ACTION], [NAME], DESCRIPTION, PERMISSION, STATUS, OCCURRED_ON, OCCURRED_ON_CET from
    (
        (
            select
                user_role.Id as ID,
                user_role.Name as USER_ROLE,
                executing_user.Email as EXECUTING_USER_MAIL,
                'CHANGED_BASIC_DATA' as [ACTION],
                1 as ACTION_ORDER,
                user_role.Name as [NAME],
                user_role.Description as DESCRIPTION,
                case
                    when user_role.Status = 1 then 'ACTIVE'
                    when user_role.Status = 2 then 'INACTIVE'
                end as STATUS,
                '' as PERMISSION,
                case
                    when mr.EicFunction = 1 then 'BalanceResponsibleParty'
                    when mr.EicFunction = 3 then 'BillingAgent'
                    when mr.EicFunction = 12 then 'EnergySupplier'
                    when mr.EicFunction = 14 then 'GridAccessProvider'
                    when mr.EicFunction = 15 then 'ImbalanceSettlementResponsible'
                    when mr.EicFunction = 22 then 'MeterOperator'
                    when mr.EicFunction = 23 then 'MeteredDataAdministrator'
                    when mr.EicFunction = 26 then 'MeteredDataResponsible'
                    when mr.EicFunction = 27 then 'MeteringPointAdministrator'
                    when mr.EicFunction = 45 then 'SystemOperator'
                    when mr.EicFunction = 48 then 'DanishEnergyAgency'
                    when mr.EicFunction = 50 then 'DataHubAdministrator'
                    when mr.EicFunction = 51 then 'IndependentAggregator'
                    when mr.EicFunction = 52 then 'SerialEnergyTrader'
                    when mr.EicFunction = 53 then 'Delegated'
                    when mr.EicFunction = 54 then 'ItSupplier'
                    else CAST(mr.EicFunction as varchar)
                end as MARKET_ROLE,
                user_role.PeriodStart as OCCURRED_ON,
                FORMAT(user_role.PeriodStart at TIME ZONE 'UTC' at TIME ZONE 'Central European Standard Time', 'yyyy-MM-dd HH:mm') as OCCURRED_ON_CET
            from [dbo].[UserRole] user_role
                join [dbo].[User] executing_user on user_role.ChangedByIdentityId = executing_user.Id
                join [dbo].[UserRoleEicFunction] mr on user_role.Id = mr.UserRoleId
        )
        union
        (
            select
                user_role_history.Id as ID,
                ur.Name as USER_ROLE,
                executing_user.Email as EXECUTING_USER_MAIL,
                'CHANGED_BASIC_DATA' as [ACTION],
                1 as ACTION_ORDER,
                user_role_history.Name as [NAME],
                user_role_history.Description as DESCRIPTION,
                case
                    when user_role_history.Status = 1 then 'ACTIVE'
                    when user_role_history.Status = 2 then 'INACTIVE'
                end as STATUS,
                '' as PERMISSION,
                case
                    when mr.EicFunction = 1 then 'BalanceResponsibleParty'
                    when mr.EicFunction = 3 then 'BillingAgent'
                    when mr.EicFunction = 12 then 'EnergySupplier'
                    when mr.EicFunction = 14 then 'GridAccessProvider'
                    when mr.EicFunction = 15 then 'ImbalanceSettlementResponsible'
                    when mr.EicFunction = 22 then 'MeterOperator'
                    when mr.EicFunction = 23 then 'MeteredDataAdministrator'
                    when mr.EicFunction = 26 then 'MeteredDataResponsible'
                    when mr.EicFunction = 27 then 'MeteringPointAdministrator'
                    when mr.EicFunction = 45 then 'SystemOperator'
                    when mr.EicFunction = 48 then 'DanishEnergyAgency'
                    when mr.EicFunction = 50 then 'DataHubAdministrator'
                    when mr.EicFunction = 51 then 'IndependentAggregator'
                    when mr.EicFunction = 52 then 'SerialEnergyTrader'
                    when mr.EicFunction = 53 then 'Delegated'
                    when mr.EicFunction = 54 then 'ItSupplier'
                    else CAST(mr.EicFunction as varchar)
                end as MARKET_ROLE,
                user_role_history.PeriodStart as OCCURRED_ON,
                FORMAT(user_role_history.PeriodStart at TIME ZONE 'UTC' at TIME ZONE 'Central European Standard Time', 'yyyy-MM-dd HH:mm') as OCCURRED_ON_CET
            from [dbo].[UserRoleHistory] user_role_history
                join [dbo].[User] executing_user on user_role_history.ChangedByIdentityId = executing_user.Id
                join [dbo].[UserRoleEicFunction] mr on user_role_history.Id = mr.UserRoleId
                join [dbo].[UserRole] ur on user_role_history.Id = ur.Id
        )
        union
        (
            select
                user_role_permission.UserRoleId as ID,
                ur.Name as USER_ROLE,
                executing_user.Email as EXECUTING_USER_MAIL,
                case
                    when user_role_permission.DeletedByIdentityId is null then 'ADDED_PERMISSION'
                    when user_role_permission.DeletedByIdentityId is not null then 'REMOVED_PERMISSION'
                end as [ACTION],
                2 as ACTION_ORDER,
                '' as [NAME],
                '' as DESCRIPTION,
                '' as STATUS,
                case
                    when user_role_permission.PermissionId = 1 then 'OrganizationView'
                    when user_role_permission.PermissionId = 2 then 'OrganizationManage'
                    when user_role_permission.PermissionId = 3 then 'GridAreasManage'
                    when user_role_permission.PermissionId = 4 then 'ActorsManage'
                    when user_role_permission.PermissionId = 5 then 'UsersManage'
                    when user_role_permission.PermissionId = 6 then 'UsersView'
                    when user_role_permission.PermissionId = 7 then 'UserRolesManage'
                    when user_role_permission.PermissionId = 8 then 'ImbalancePricesManage'
                    when user_role_permission.PermissionId = 9 then 'CalculationsManage'
                    when user_role_permission.PermissionId = 10 then 'SettlementReportsManage'
                    when user_role_permission.PermissionId = 11 then 'ESettExchangeManage'
                    when user_role_permission.PermissionId = 12 then 'RequestAggregatedMeasureData'
                    when user_role_permission.PermissionId = 13 then 'ActorCredentialsManage'
                    when user_role_permission.PermissionId = 14 then 'ActorMasterDataManage'
                    when user_role_permission.PermissionId = 15 then 'DelegationView'
                    when user_role_permission.PermissionId = 16 then 'DelegationManage'
                    when user_role_permission.PermissionId = 17 then 'UsersReActivate'
                    when user_role_permission.PermissionId = 18 then 'BalanceResponsibilityView'
                    when user_role_permission.PermissionId = 19 then 'RequestWholesaleSettlement'
                    when user_role_permission.PermissionId = 20 then 'CalculationsView'
                    when user_role_permission.PermissionId = 21 then 'ImbalancePricesView'
                    else CAST(user_role_permission.PermissionId as varchar)
                end as PERMISSION,
                case
                    when mr.EicFunction = 1 then 'BalanceResponsibleParty'
                    when mr.EicFunction = 3 then 'BillingAgent'
                    when mr.EicFunction = 12 then 'EnergySupplier'
                    when mr.EicFunction = 14 then 'GridAccessProvider'
                    when mr.EicFunction = 15 then 'ImbalanceSettlementResponsible'
                    when mr.EicFunction = 22 then 'MeterOperator'
                    when mr.EicFunction = 23 then 'MeteredDataAdministrator'
                    when mr.EicFunction = 26 then 'MeteredDataResponsible'
                    when mr.EicFunction = 27 then 'MeteringPointAdministrator'
                    when mr.EicFunction = 45 then 'SystemOperator'
                    when mr.EicFunction = 48 then 'DanishEnergyAgency'
                    when mr.EicFunction = 50 then 'DataHubAdministrator'
                    when mr.EicFunction = 51 then 'IndependentAggregator'
                    when mr.EicFunction = 52 then 'SerialEnergyTrader'
                    when mr.EicFunction = 53 then 'Delegated'
                    when mr.EicFunction = 54 then 'ItSupplier'
                    else CAST(mr.EicFunction as varchar)
                end as MARKET_ROLE,
                user_role_permission.PeriodStart as OCCURRED_ON,
                FORMAT(user_role_permission.PeriodStart at TIME ZONE 'UTC' at TIME ZONE 'Central European Standard Time', 'yyyy-MM-dd HH:mm') as OCCURRED_ON_CET
            from [dbo].[UserRolePermission] user_role_permission
                join [dbo].[User] executing_user on
                    (user_role_permission.ChangedByIdentityId = executing_user.Id and user_role_permission.DeletedByIdentityId is null) or
                    (user_role_permission.DeletedByIdentityId = executing_user.Id)
                join [dbo].[UserRoleEicFunction] mr on user_role_permission.UserRoleId = mr.UserRoleId
                join [dbo].[UserRole] ur on user_role_permission.UserRoleId = ur.Id
        )
        union
        (
            select
                user_role_permission_history.UserRoleId as ID,
                ur.Name as USER_ROLE,
                executing_user.Email as EXECUTING_USER_MAIL,
                case
                    when user_role_permission_history.DeletedByIdentityId is null then 'ADDED_PERMISSION'
                    when user_role_permission_history.DeletedByIdentityId is not null then 'REMOVED_PERMISSION'
                end as [ACTION],
                2 as ACTION_ORDER,
                '' as [NAME],
                '' as DESCRIPTION,
                '' as STATUS,
                case
                    when user_role_permission_history.PermissionId = 1 then 'OrganizationView'
                    when user_role_permission_history.PermissionId = 2 then 'OrganizationManage'
                    when user_role_permission_history.PermissionId = 3 then 'GridAreasManage'
                    when user_role_permission_history.PermissionId = 4 then 'ActorsManage'
                    when user_role_permission_history.PermissionId = 5 then 'UsersManage'
                    when user_role_permission_history.PermissionId = 6 then 'UsersView'
                    when user_role_permission_history.PermissionId = 7 then 'UserRolesManage'
                    when user_role_permission_history.PermissionId = 8 then 'ImbalancePricesManage'
                    when user_role_permission_history.PermissionId = 9 then 'CalculationsManage'
                    when user_role_permission_history.PermissionId = 10 then 'SettlementReportsManage'
                    when user_role_permission_history.PermissionId = 11 then 'ESettExchangeManage'
                    when user_role_permission_history.PermissionId = 12 then 'RequestAggregatedMeasureData'
                    when user_role_permission_history.PermissionId = 13 then 'ActorCredentialsManage'
                    when user_role_permission_history.PermissionId = 14 then 'ActorMasterDataManage'
                    when user_role_permission_history.PermissionId = 15 then 'DelegationView'
                    when user_role_permission_history.PermissionId = 16 then 'DelegationManage'
                    when user_role_permission_history.PermissionId = 17 then 'UsersReActivate'
                    when user_role_permission_history.PermissionId = 18 then 'BalanceResponsibilityView'
                    when user_role_permission_history.PermissionId = 19 then 'RequestWholesaleSettlement'
                    when user_role_permission_history.PermissionId = 20 then 'CalculationsView'
                    when user_role_permission_history.PermissionId = 21 then 'ImbalancePricesView'
                    else CAST(user_role_permission_history.PermissionId as varchar)
                end as PERMISSION,
                case
                    when mr.EicFunction = 1 then 'BalanceResponsibleParty'
                    when mr.EicFunction = 3 then 'BillingAgent'
                    when mr.EicFunction = 12 then 'EnergySupplier'
                    when mr.EicFunction = 14 then 'GridAccessProvider'
                    when mr.EicFunction = 15 then 'ImbalanceSettlementResponsible'
                    when mr.EicFunction = 22 then 'MeterOperator'
                    when mr.EicFunction = 23 then 'MeteredDataAdministrator'
                    when mr.EicFunction = 26 then 'MeteredDataResponsible'
                    when mr.EicFunction = 27 then 'MeteringPointAdministrator'
                    when mr.EicFunction = 45 then 'SystemOperator'
                    when mr.EicFunction = 48 then 'DanishEnergyAgency'
                    when mr.EicFunction = 50 then 'DataHubAdministrator'
                    when mr.EicFunction = 51 then 'IndependentAggregator'
                    when mr.EicFunction = 52 then 'SerialEnergyTrader'
                    when mr.EicFunction = 53 then 'Delegated'
                    when mr.EicFunction = 54 then 'ItSupplier'
                    else CAST(mr.EicFunction as varchar)
                end as MARKET_ROLE,
                user_role_permission_history.PeriodStart as OCCURRED_ON,
                FORMAT(user_role_permission_history.PeriodStart at TIME ZONE 'UTC' at TIME ZONE 'Central European Standard Time', 'yyyy-MM-dd HH:mm') as OCCURRED_ON_CET
            from [dbo].[UserRolePermissionHistory] user_role_permission_history
                join [dbo].[User] executing_user on
                    (user_role_permission_history.ChangedByIdentityId = executing_user.Id and user_role_permission_history.DeletedByIdentityId is null) or
                    (user_role_permission_history.DeletedByIdentityId = executing_user.Id)
                join [dbo].[UserRoleEicFunction] mr on user_role_permission_history.UserRoleId = mr.UserRoleId
                join [dbo].[UserRole] ur on user_role_permission_history.UserRoleId = ur.Id
        )
    ) as audit_log
    where @FromDateInclusive <= OCCURRED_ON and OCCURRED_ON < @ToDateExclusive
    order by OCCURRED_ON, ID, ACTION_ORDER
END;
