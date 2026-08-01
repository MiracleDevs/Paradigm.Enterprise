CREATE TABLE [dbo].[QuoteLine]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [QuoteId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [SkuSnapshot] NVARCHAR(32) NOT NULL,
    [ProductNameSnapshot] NVARCHAR(120) NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPrice] DECIMAL(19,4) NOT NULL,
    [DiscountPercent] DECIMAL(5,2) NOT NULL,
    [LineSubtotal] AS CONVERT(DECIMAL(19,2), ROUND(CONVERT(DECIMAL(38,6), [Quantity]) * [UnitPrice], 2)) PERSISTED,
    [DiscountAmount] AS CONVERT(DECIMAL(19,2), ROUND(ROUND(CONVERT(DECIMAL(38,6), [Quantity]) * [UnitPrice], 2) * [DiscountPercent] / 100, 2)) PERSISTED,
    [LineTotal] AS CONVERT(DECIMAL(19,2), ROUND(CONVERT(DECIMAL(38,6), [Quantity]) * [UnitPrice], 2) - ROUND(ROUND(CONVERT(DECIMAL(38,6), [Quantity]) * [UnitPrice], 2) * [DiscountPercent] / 100, 2)) PERSISTED,
    CONSTRAINT [PK_QuoteLine] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_QuoteLine_QuoteId_ProductId] UNIQUE ([QuoteId], [ProductId]),
    CONSTRAINT [FK_QuoteLine_Quote] FOREIGN KEY ([QuoteId]) REFERENCES [dbo].[Quote] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QuoteLine_Product] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Product] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_QuoteLine_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_QuoteLine_UnitPrice] CHECK ([UnitPrice] >= 0),
    CONSTRAINT [CK_QuoteLine_DiscountPercent] CHECK ([DiscountPercent] BETWEEN 0 AND 100)
);
GO
CREATE INDEX [IX_QuoteLine_QuoteId_Id] ON [dbo].[QuoteLine] ([QuoteId], [Id]);
