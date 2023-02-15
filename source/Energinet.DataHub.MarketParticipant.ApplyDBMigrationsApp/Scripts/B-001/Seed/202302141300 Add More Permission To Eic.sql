--Permission.OrganizationView
INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('fe180600-73c8-4755-841c-6d684c30f78d'
        ,1
        ,1),
    ('32b315b7-63ab-4693-85f1-7e9b29947e0c'
        ,1
        ,3),
    ('ec072bfe-f527-4a1e-aed9-de307decea05'
        ,1
        ,14),
    ('51a5c2dc-8580-4000-a293-1a4b237c60c6'
        ,1
        ,15),
    ('26c030bf-a2e1-493c-99c4-0b12ad52661f'
        ,1
        ,23),
    ('9f87a86f-1ec5-42e4-a23b-951254da57d1'
        ,1
        ,26),
    ('59ac6d21-913f-4b3d-af00-b3986b76f5fe'
        ,1
        ,27),
    ('30d3400b-f7b4-4a64-9d58-df5ce3129bfa'
        ,1
        ,45),
    ('300c5d3b-c268-4b3b-bc12-a5716e133dca'
        ,1
        ,48),
    ('d4d29e70-180d-4c74-8183-4a1e4caf4906'
        ,1
        ,49),
    ('82b9c736-f526-4cd9-ba8e-4a8b0788f51f'
        ,1
        ,50)
        ,
    ('4554132f-c979-4d8e-a395-dee9ad9f200f'
        ,1
        ,51),
    ('54990de9-128f-48fe-bdff-fc653c7c07c6'
        ,1
        ,52)

--Permission.OrganizationManage
DELETE FROM
    [dbo].[PermissionEicFunction]
WHERE
    [PermissionId] = 2

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('89ca9ef3-1a2c-4adc-9133-289e528290a3'
        ,2
        ,50)

--Permission.GridAreasManage
DELETE FROM
    [dbo].[PermissionEicFunction]
WHERE
    [PermissionId] = 3

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('a1fc2559-2ba0-4482-b3eb-9947c9bc3309'
        ,3
        ,50)

--Permission.ActorManage
DELETE FROM
    [dbo].[PermissionEicFunction]
WHERE
    [PermissionId] = 4

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('2f86cea8-654c-4796-8d2d-cd1cf0055148'
        ,4
        ,50)


--Permission.UsersManage
INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('97cd2661-e445-4e91-83e8-103e538043de'
        ,5
        ,1),
    ('e54d4d65-ff40-4d49-aea9-c8bfbd341d20'
        ,5
        ,14),
    ('f243f7bc-92d3-4a5a-ab8f-9b9e71aba60d'
        ,5
        ,15),
    ('173a9f19-ff40-41f3-a353-7541a096ea6f'
        ,5
        ,23),
    ('dd72dca5-a028-41a5-bf25-eeec47f4b8ce'
        ,5
        ,26),
    ('93c58710-3d74-49ba-9732-ea22f9e895de'
        ,5
        ,27),
    ('1289e636-11fc-4dd3-8053-4e53f6d34557'
        ,5
        ,45),
    ('46c6f224-a29e-4048-aafa-6e5e8812ebe3'
        ,5
        ,48),
    ('83442b19-c3b2-49fb-9c21-1994bd642e44'
        ,5
        ,49),
    ('2b93ee0b-9e53-4ef7-ab07-5721946fe05a'
        ,5
        ,50),
    ('10346739-0182-4c83-9286-1633271b8f9b'
        ,5
        ,51),
    ('dec621e7-23f5-4a73-9d6d-93cae2e8e0f2'
        ,5
        ,52)

--Permission.UsersView
INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('2db5faff-3435-4a8d-bc73-812ba41d197d'
        ,6
        ,1),
    ('1ef43b82-6eda-4f30-afd0-81980f1529d3'
        ,6
        ,14),
    ('1a0dbd1a-2aeb-410b-a1c7-d71aaf3ef3b3'
        ,6
        ,15),
    ('d3022e96-f2c9-4823-9035-c7d71f647094'
        ,6
        ,23),
    ('a4b8a80a-9977-4e33-b02f-abe2bbb3cfa0'
        ,6
        ,26),
    ('7ee52cad-6209-4cab-8ed2-f40f8487b710'
        ,6
        ,27),
    ('e7bd1807-2208-4b38-901b-524cf315cd98'
        ,6
        ,45),
    ('276bfba7-1756-4df2-b1e1-12f09869e9d4'
        ,6
        ,48),
    ('48adb4cd-98bd-4f40-9f9b-f31468a58755'
        ,6
        ,49),
    ('8e9950e7-69ef-401f-8105-d891c7318629'
        ,6
        ,50),
    ('e36cdef0-97e0-4e94-a55e-82b56500de6a'
        ,6
        ,51),
    ('7330a27b-19de-433e-b500-3b08edb59c4a'
        ,6
        ,52)

--Permission.UserRoleManage
DELETE FROM
    [dbo].[PermissionEicFunction]
WHERE
    [PermissionId] = 7

INSERT INTO [dbo].[PermissionEicFunction]
([Id]
    ,[PermissionId]
    ,[EicFunction])
VALUES
    ('7a904316-b52b-4e6f-8502-39b07642e5c8'
        ,7
        ,50)
    GO