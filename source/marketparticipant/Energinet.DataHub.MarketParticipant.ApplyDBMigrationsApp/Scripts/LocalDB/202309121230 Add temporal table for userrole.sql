/* UserRole Temporal table setup */
ALTER TABLE [UserRole]
    ADD
    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_UserRole_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_UserRole_PeriodEnd DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
    ChangedByIdentityId uniqueidentifier NOT NULL
	CONSTRAINT DF_ChangedByIdentityId_defaultvalue DEFAULT('00000000-0000-0000-0000-000000000000');
GO

ALTER TABLE UserRole
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[UserRoleHistory]));
GO

ALTER TABLE [UserRole] DROP CONSTRAINT DF_ChangedByIdentityId_defaultvalue
GO

/* UserRolePermission Temporal table setup */
ALTER TABLE [UserRolePermission]
    ADD
    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_UserRolePermission_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_UserRolePermission_PeriodEnd DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
	CreatedByIdentityId uniqueidentifier NOT NULL
	CONSTRAINT DF_ChangedByIdentityId_defaultvalue DEFAULT('00000000-0000-0000-0000-000000000000');
    DeletedByIdentityId uniqueidentifier NULL                                   
GO

ALTER TABLE [UserRolePermission]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[UserRolePermissionHistory]));
GO

ALTER TABLE [UserRolePermission] DROP CONSTRAINT DF_ChangedByIdentityId_defaultvalue
GO
