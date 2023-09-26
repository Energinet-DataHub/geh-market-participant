/* UserRole Temporal table setup */
ALTER TABLE [dbo].[UserRole]
    ADD
    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_UserRole_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_UserRole_PeriodEnd DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
    ChangedByIdentityId uniqueidentifier NOT NULL
	CONSTRAINT DF_ChangedByIdentityId DEFAULT('00000000-FFFF-FFFF-FFFF-000000000000');
GO

ALTER TABLE [dbo].[UserRole]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[UserRoleHistory]));
GO

ALTER TABLE [dbo].[UserRole] DROP CONSTRAINT DF_ChangedByIdentityId
GO

ALTER TABLE [dbo].[UserRole]
    ADD CONSTRAINT CHK_UserRole_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000');
GO

/* UserRolePermission Temporal table setup */
ALTER TABLE [dbo].[UserRolePermission]
    ADD
    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_UserRolePermission_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_UserRolePermission_PeriodEnd DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
	ChangedByIdentityId uniqueidentifier NOT NULL
	CONSTRAINT DF_ChangedByIdentityId DEFAULT('00000000-FFFF-FFFF-FFFF-000000000000'),
    DeletedByIdentityId uniqueidentifier NULL                                   
GO

ALTER TABLE [dbo].[UserRolePermission]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[UserRolePermissionHistory]));
GO

ALTER TABLE [UserRolePermission] DROP CONSTRAINT DF_ChangedByIdentityId
GO

ALTER TABLE [dbo].[UserRolePermission]
    ADD CONSTRAINT CHK_UserRolePermission_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000');
GO
