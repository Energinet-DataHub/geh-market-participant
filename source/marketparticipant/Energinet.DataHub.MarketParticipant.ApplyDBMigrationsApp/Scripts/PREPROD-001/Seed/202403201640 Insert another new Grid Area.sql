INSERT INTO [dbo].[GridArea] ([Id], [Code], [Name], [PriceAreaCode], [ValidFrom], [FullFlexDate], [ChangedByIdentityId])
VALUES
    (NEWID(), '484',   '',                 2, CONVERT(datetime, '02-01-1950 00:00:00', 105), CONVERT(datetime, '01-02-2021 00:00:00', 105), '00000000-FFFF-FFFF-FFFF-000000000000')