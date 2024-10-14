INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [Type], [PriceAreaCode], [ValidFrom], [FullFlexDate], [ChangedByIdentityId])
VALUES
    (NEWID(), '900', 'KMD Elem 900', 1, 2, CONVERT(datetime, '01-01-1950 00:00:00', 105), CONVERT(datetime, '01-02-2021 00:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '902', 'KMD Elem 902', 1, 2, CONVERT(datetime, '01-01-1950 00:00:00', 105), CONVERT(datetime, '01-02-2021 00:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000')
