-- Data for the link to Eic Functions match U-001 at 02-02-2023
-- If no Eic function is set for a permission, it will be added to a Default
-- This has been chosen to be BillingAgent

--Permission.OrganizationView
INSERT INTO [dbo].[Permission]
([Id]
    ,[Description])
VALUES
    (1
        ,'Description for OrganizationView')
    GO

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('c91d0c7c-ebd2-4adf-ab3a-be7cbb91e3e5'
        ,1
        ,12)
    GO

--Permission.OrganizationManage
INSERT INTO [dbo].[Permission]
([Id]
    ,[Description])
VALUES
    (2
        ,'Description for OrganizationManage')
    GO

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('620f802f-4aa9-42f0-b0f9-67195c3e7371'
        ,2
        ,3),
    ('1a274e0f-53ef-46bb-a9e2-6aa473070892'
        ,2
        ,12)
    GO

--Permission.GridAreasManage
INSERT INTO [dbo].[Permission]
([Id]
    ,[Description])
VALUES
    (3
        ,'Description for GridAreasManage')
    GO

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('93be9683-7149-476e-873e-d475c11f8341'
        ,3
        ,2),
    ('59d74756-c490-45e5-b9e3-7cd7d9fb995a'
        ,3
        ,12)
    GO

--Permission.ActorManage
INSERT INTO [dbo].[Permission]
([Id]
    ,[Description])
VALUES
    (4
        ,'Description for ActorManage')
    GO

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('e3acf7f0-6e1b-41eb-830b-f821e1c6b5b5'
        ,4
        ,3),
    ('78be652d-b488-43d6-9c27-db7cefb9d3de'
        ,4
        ,12)
    GO

--Permission.UsersManage
INSERT INTO [dbo].[Permission]
([Id]
    ,[Description])
VALUES
    (5
        ,'Description for UsersManage')
    GO

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('ffbcf5c3-f43b-4273-b3da-1a5fa1b3d196'
        ,5
        ,3),
    ('fee620be-94a1-4a64-af75-9387a1cc14bb'
        ,5
        ,12)
    GO

--Permission.UsersView
INSERT INTO [dbo].[Permission]
([Id]
    ,[Description])
VALUES
    (6
        ,'Description for UsersView')
    GO

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('de12c91a-8934-41ed-96a5-5f2e91dd811f'
        ,6
        ,3)
    GO

--Permission.UserRoleManage
INSERT INTO [dbo].[Permission]
([Id]
    ,[Description])
VALUES
    (7
        ,'Description for UserRoleManage')
    GO

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('497f68dc-63b1-4cb8-a206-0b8e3f3a905f'
        ,7
        ,12)
    GO