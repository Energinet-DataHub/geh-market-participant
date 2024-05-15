INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [PriceAreaCode], [ValidFrom], [FullFlexDate], [ChangedByIdentityId])
VALUES
    (NEWID(), '003', 'Netområde 003', 1, CONVERT(datetime, '21-02-2018 23:00:00', 105), CONVERT(datetime, '31-12-2019 23:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '007', 'Netområde 007', 2, CONVERT(datetime, '21-02-2018 23:00:00', 105), CONVERT(datetime, '31-12-2019 23:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '543', 'Netområde 543', 1, CONVERT(datetime, '21-02-2018 23:00:00', 105), CONVERT(datetime, '31-12-2019 23:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000')
