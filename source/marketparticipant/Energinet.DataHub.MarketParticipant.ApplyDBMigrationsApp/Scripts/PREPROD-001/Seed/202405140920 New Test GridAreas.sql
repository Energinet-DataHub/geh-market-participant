INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [PriceAreaCode], [ValidFrom], [FullFlexDate], [ChangedByIdentityId])
VALUES 
    (NEWID(), '903', 'Net - 903', 1, CONVERT(datetime, '01-01-1950 00:00:00', 105), CONVERT(datetime, '01-01-1950 00:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '939', 'Net - 939', 1, CONVERT(datetime, '01-01-1950 00:00:00', 105), CONVERT(datetime, '01-02-2021 00:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '989', 'Net - 989', 1, CONVERT(datetime, '01-01-1950 00:00:00', 105), CONVERT(datetime, '01-02-2021 00:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000')
