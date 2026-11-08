/* Additive migration for existing FlashSaleStore databases. */
USE [FlashSaleStore];
GO

IF COL_LENGTH(N'dbo.Products', N'IsFeatured') IS NULL
BEGIN
    ALTER TABLE dbo.Products
        ADD IsFeatured bit NOT NULL
            CONSTRAINT DF_Products_IsFeatured DEFAULT 0;
END;
GO

IF COL_LENGTH(N'dbo.Payments', N'RequiresManualRefund') IS NULL
BEGIN
    ALTER TABLE dbo.Payments
        ADD RequiresManualRefund bit NOT NULL
            CONSTRAINT DF_Payments_ManualRefund DEFAULT 0;
END;
GO
