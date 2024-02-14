INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [PriceAreaCode], [ValidFrom], [FullFlexDate], [ChangedByIdentityId])
VALUES
    (NEWID(), '990', 'Netområde 990', 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2020 23:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000')
