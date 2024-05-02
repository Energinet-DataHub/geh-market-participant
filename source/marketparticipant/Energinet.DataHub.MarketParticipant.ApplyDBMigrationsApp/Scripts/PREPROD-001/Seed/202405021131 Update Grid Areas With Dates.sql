UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '003'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '007'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '031'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '042'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '051'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '084'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '085'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '131'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '141'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '151'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '154'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '244'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '245'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '312'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '331'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '341'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '342'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '344'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '347'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '348'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '351'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '357'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '370'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '371'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '381'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '384'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '385'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '396'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '531'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '532'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '533'
UPDATE [dbo].[GridArea] SET [ValidFrom] = CONVERT(datetime, '01-01-1900 00:00:00', 105) WHERE [Code] = '543'

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
    (NEWID(), '149', 'N1 A/S - 149 (ELRO)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '152', 'N1 A/S - 152',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '28-02-2023 23:00:00', 105), CONVERT(datetime, '30-11-2020 23:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '232', 'Dinel A/S - 232 (Østjysk)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2017 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '246', 'MES Net A/S',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '28-02-2018 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '248', 'VOS Net A/S',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '353', 'N1 A/S - 353 (Vest)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '359', 'RAH Net A/S - 359',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-10-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '392', 'N1 A/S - 392 (Borris)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '394', 'Kibæk Elværk Amba',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2019 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '397', 'N1 A/S - 397 (Sdr. Felding)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '398', 'N1 A/S - 398',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-01-2024 23:00:00', 105), CONVERT(datetime, '30-11-2020 23:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '443', 'Ærø Elforsyning Net A/S',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-12-2017 23:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '512', 'Vores Elnet A/S - 512',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '28-02-2022 23:00:00', 105), CONVERT(datetime, '31-12-2019 23:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '552', 'Vores Elnet A/S - 552 (FFV)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2020 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '553', 'Vores Elnet A/S - 553 (Nyborg)',                 1, CONVERT(datetime, '01-01-1900 00:00:00', 105), CONVERT(datetime, '31-03-2020 22:00:00', 105), null, '00000000-FFFF-FFFF-FFFF-000000000000'),

