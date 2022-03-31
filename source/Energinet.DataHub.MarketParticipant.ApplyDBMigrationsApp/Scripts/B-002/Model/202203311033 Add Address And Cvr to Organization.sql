ALTER TABLE [dbo].[OrganizationInfo]
    ADD Cvr nvarchar(8),
        Address_StreetName nvarchar(250),
        Address_Number nvarchar(15),
        Address_ZipCode nvarchar(15),
        Address_City nvarchar(50),
        Address_Country nvarchar(50)
GO