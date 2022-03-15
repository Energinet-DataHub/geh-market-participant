INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'bb71f037-692d-4bcd-9c79-741ad90e33ca', N'8100000000200', 1, N'DDM,MDR,DDQ', 1, N'Titans T-001')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'e52427c2-204b-4cf2-beec-35b95f935619', N'8100000000201', 1, N'DDM,MDR,DDQ', 1, N'Batman T-001')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'950af11e-85dd-49da-943f-491c7730ff7a', N'8100000000202', 1, N'DDM,MDR', 1, N'endk-DDM8')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'5ed12387-2926-4a93-a76c-e779d2b54f4a', N'8100000000203', 1, N'DDM,MDR', 1, N'MD T-001')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'c2059ddf-ad04-4a99-bc98-2c6fde857025', N'8100000000204', 1, N'DDM,MDR', 1, N'Joules T-001')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'a6e88928-58c3-458e-92bc-3f2a56dbd09a', N'8100000000205', 1, N'DDM,MDR', 1, N'endk-DDM2')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'0a05bb66-42b8-4b71-affb-9a3716a94489', N'8100000000206', 1, N'DDM,MDR', 1, N'endk-DDM5')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'ef69b130-1ead-4e58-a926-a2b1154d7e2a', N'8100000000207', 1, N'DDM,MDR', 1, N'Volt T-001')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'893ab294-59c1-4c81-add7-0ae7ef80912c', N'8100000000208', 1, N'DDM,MDR', 1, N'endk-DDM3')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'31e83b3a-22d3-49ce-bdf1-2d456205f0d1', N'8100000000209', 1, N'DDM,MDR', 1, N'endk-DDM6')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'2ab0e215-537f-4de1-b609-09b9777e31dd', N'8100000000210', 1, N'EZ', 1, N'endk-TSO')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'5806501c-d376-4859-8376-e2886ce36b90', N'8100000000211', 1, N'DDM,MDR', 1, N'endk-DDM1')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'55878bd4-ef25-4f2e-8ba8-b2f3cca962e6', N'8100000000212', 1, N'DDM,MDR', 1, N'endk-DDM7')

INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name])
VALUES (N'd2eabcd3-3ed1-4dea-a5be-f043b703dac0', N'8100000000213', 1, N'DDM,MDR', 1, N'endk-DDM4')
GO

--Add the new grid Areas
INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (n'2285dcb4-0bdd-4ec9-90cc-d757a062fb86', 34, N'116', N'KMDTest4', N'DK1', 1,
        N'bb71f037-692d-4bcd-9c79-741ad90e33ca')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'62b86e5f-0216-4315-a3d5-a5523652c183', 35, N'117', N'KMDTest5', N'DK1', 1,
        N'e52427c2-204b-4cf2-beec-35b95f935619')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'910f07a4-3a64-4871-96f2-46e11c706858', 36, N'118', N'KMDTest6', N'DK1', 1,
        N'950af11e-85dd-49da-943f-491c7730ff7a')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (n'f1b0d811-c334-44de-b5ba-79f17b807fbc', 37, N'119', N'KMDTest4', N'DK1', 1,
        N'5ed12387-2926-4a93-a76c-e779d2b54f4a')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'e2b2ea7b-4300-4cb0-93cc-ee33ab8f3b30', 38, N'120', N'KMDTest5', N'DK1', 1,
        N'c2059ddf-ad04-4a99-bc98-2c6fde857025')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'ec23e895-eccb-4511-8970-f65804bde688', 39, N'121', N'KMDTest6', N'DK1', 1,
        N'a6e88928-58c3-458e-92bc-3f2a56dbd09a')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (n'edb7e8a2-ce75-43e1-9953-890ca85db71d', 40, N'122', N'KMDTest4', N'DK1', 1,
        N'0a05bb66-42b8-4b71-affb-9a3716a94489')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'd45f9498-1954-4c7d-8e9c-0d4a2aba058b', 41, N'123', N'KMDTest5', N'DK1', 1,
        N'ef69b130-1ead-4e58-a926-a2b1154d7e2a')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'6a6fff54-41dc-4c00-ab9f-57ade4625afc', 42, N'124', N'KMDTest6', N'DK1', 1,
        N'893ab294-59c1-4c81-add7-0ae7ef80912c')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (n'15bc0fbe-1ee2-476b-87df-b6d737bc527a', 43, N'125', N'KMDTest4', N'DK1', 1,
        N'31e83b3a-22d3-49ce-bdf1-2d456205f0d1')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'12cc1456-ec8b-42ef-af24-c06d016d9b8d', 44, N'126', N'KMDTest5', N'DK1', 1,
        N'5806501c-d376-4859-8376-e2886ce36b90')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'ec68094b-3b47-4429-9639-7e4e74fb42d2', 45, N'127', N'KMDTest6', N'DK1', 1,
        N'55878bd4-ef25-4f2e-8ba8-b2f3cca962e6')

INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId])
VALUES (N'f1294a65-18d9-40d2-9996-19fd4c9636e9', 46, N'128', N'KMDTest6', N'DK1', 1,
        N'd2eabcd3-3ed1-4dea-a5be-f043b703dac0')
GO

--Add Grid Area Links
SET IDENTITY_INSERT [dbo].[GridAreaLinkInfo] ON

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'2ae8f968-f798-4379-a667-60391dfbbfb4', N'2285dcb4-0bdd-4ec9-90cc-d757a062fb86', 34)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'8b70e867-8fe3-4680-bb25-09d11624f3f1', N'62b86e5f-0216-4315-a3d5-a5523652c183', 35)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'c7d8210f-c342-4b29-989a-1c14f3a48c93', N'910f07a4-3a64-4871-96f2-46e11c706858', 36)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'992265b3-8ee1-44e1-a75d-46561eb59575', N'f1b0d811-c334-44de-b5ba-79f17b807fbc', 37)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'c86fd83f-c8ec-45a0-8915-54aba3b99e4e', N'e2b2ea7b-4300-4cb0-93cc-ee33ab8f3b30', 38)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'59cd37cf-406b-4f55-9d28-65dec3f6e404', N'ec23e895-eccb-4511-8970-f65804bde688', 39)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'6111058f-132f-4e02-a151-35f6b04b5093', N'edb7e8a2-ce75-43e1-9953-890ca85db71d', 40)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'39a4c1da-3d20-43be-a84b-92361e5dde6a', N'd45f9498-1954-4c7d-8e9c-0d4a2aba058b', 41)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'a26e30d0-c654-442e-9325-ea2bda3a4aec', N'6a6fff54-41dc-4c00-ab9f-57ade4625afc', 42)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'fe70d76f-a29e-4d2b-a14d-49d3ce042bca', N'15bc0fbe-1ee2-476b-87df-b6d737bc527a', 43)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'b2c61bed-aa05-4ca2-a23e-0165c23e12f2', N'12cc1456-ec8b-42ef-af24-c06d016d9b8d', 44)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'a99ee6bb-3f8f-4030-8f1b-0c46e20de9d4', N'ec68094b-3b47-4429-9639-7e4e74fb42d2', 45)

INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId])
VALUES (N'70b700f3-2263-49cc-affd-c91182acdb7c', N'f1294a65-18d9-40d2-9996-19fd4c9636e9', 46)

SET IDENTITY_INSERT [dbo].[GridAreaLinkInfo] OFF
GO