/* Run once after the base FlashSaleStore schema has been deployed. */
IF COL_LENGTH(N'dbo.Orders', N'Province') IS NULL
    ALTER TABLE dbo.Orders ADD Province nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.Orders', N'District') IS NULL
    ALTER TABLE dbo.Orders ADD District nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.Orders', N'Ward') IS NULL
    ALTER TABLE dbo.Orders ADD Ward nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.Orders', N'AddressLine') IS NULL
    ALTER TABLE dbo.Orders ADD AddressLine nvarchar(300) NULL;
IF COL_LENGTH(N'dbo.Orders', N'Note') IS NULL
    ALTER TABLE dbo.Orders ADD Note nvarchar(500) NULL;
GO

UPDATE dbo.Orders
SET AddressLine = ShippingAddress
WHERE AddressLine IS NULL AND ShippingAddress IS NOT NULL;
GO
