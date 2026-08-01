CREATE TABLE [dbo].[SalesOrderLine]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SalesOrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [SkuSnapshot] NVARCHAR(32) NOT NULL,
    [ProductNameSnapshot] NVARCHAR(120) NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPrice] DECIMAL(19,4) NOT NULL,
    [DiscountPercent] DECIMAL(5,2) NOT NULL,
    [LineSubtotal] AS CONVERT(DECIMAL(19,2), ROUND(CONVERT(DECIMAL(38,6), [Quantity]) * [UnitPrice], 2)) PERSISTED,
    [DiscountAmount] AS CONVERT(DECIMAL(19,2), ROUND(ROUND(CONVERT(DECIMAL(38,6), [Quantity]) * [UnitPrice], 2) * [DiscountPercent] / 100, 2)) PERSISTED,
    [LineTotal] AS CONVERT(DECIMAL(19,2), ROUND(CONVERT(DECIMAL(38,6), [Quantity]) * [UnitPrice], 2) - ROUND(ROUND(CONVERT(DECIMAL(38,6), [Quantity]) * [UnitPrice], 2) * [DiscountPercent] / 100, 2)) PERSISTED,
    CONSTRAINT [PK_SalesOrderLine] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_SalesOrderLine_SalesOrderId_ProductId] UNIQUE ([SalesOrderId], [ProductId]),
    CONSTRAINT [FK_SalesOrderLine_SalesOrder] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrder] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrderLine_Product] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Product] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_SalesOrderLine_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_SalesOrderLine_UnitPrice] CHECK ([UnitPrice] >= 0),
    CONSTRAINT [CK_SalesOrderLine_DiscountPercent] CHECK ([DiscountPercent] BETWEEN 0 AND 100)
);
GO
CREATE INDEX [IX_SalesOrderLine_SalesOrderId_Id] ON [dbo].[SalesOrderLine] ([SalesOrderId], [Id]);
