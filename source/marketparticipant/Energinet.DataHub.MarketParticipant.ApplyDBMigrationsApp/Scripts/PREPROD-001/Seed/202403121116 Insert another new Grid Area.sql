INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [PriceAreaCode], [ValidFrom], [FullFlexDate], [ChangedByIdentityId])
VALUES
    (NEWID(), '904',   'Netområde',                 2, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '01-01-1950 01:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000')