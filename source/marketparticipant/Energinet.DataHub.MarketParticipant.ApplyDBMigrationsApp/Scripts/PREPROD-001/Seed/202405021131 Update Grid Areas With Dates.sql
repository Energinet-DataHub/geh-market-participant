UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '003'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '007'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '031'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '042'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '051'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '084'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '085'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '131'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '141'

-- Insert missing grid areas, that are expired
INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [PriceAreaCode], [ValidFrom], [ValidTo], [FullFlexDate], [ChangedByIdentityId])
VALUES
    (NEWID(), '014', 'Aars-Hornum Net A/S - 014 (Hornum)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-08-2017 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '015', 'Nibe Elforsyning Net A.m.b.a',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2018 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '023', 'Nyfors Net A/S',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2018 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '044', 'N1 A/S - 044 (HEF)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '052', 'N1 A/S - 052 (AKE)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '095', 'Hirtshals El-Netselskab A/S',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '096', 'Nord Energi Net A/S - 096 (Taars)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-10-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '142', 'Kjellerup Elnet A/S',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2019 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '143', 'Dinel A/S - 143 (Brabrand)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2017 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '144', 'Dinel A/S - 144 (Viby)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2017 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '145', 'Dinel A/S - 145 (Galten)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2017 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '146', 'N1 A/S - 146 (Bjerringbro)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),

