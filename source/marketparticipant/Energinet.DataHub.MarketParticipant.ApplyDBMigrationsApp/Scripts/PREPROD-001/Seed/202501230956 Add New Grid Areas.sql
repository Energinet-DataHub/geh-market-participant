INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [Type], [PriceAreaCode], [ValidFrom], [FullFlexDate], [ChangedByIdentityId])
VALUES
    (NEWID(), '921', 'Net 921', 2, 1, CONVERT(datetime, '01-01-1950 00:00:00', 105), CONVERT(datetime, '01-02-2021 00:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000'),
    (NEWID(), '922', 'Net 922', 2, 2, CONVERT(datetime, '01-01-1950 00:00:00', 105), CONVERT(datetime, '01-02-2021 00:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000')
