CREATE TABLE [dbo].[SalesOrder]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OrderNumber] NVARCHAR(20) NOT NULL,
    [SourceQuoteId] INT NULL,
    [CustomerId] INT NOT NULL,
    [ShippingAddressId] INT NOT NULL,
    [StatusId] INT NOT NULL,
    [RequestedShipDate] DATE NULL,
    [CarrierId] INT NULL,
    [TrackingNumber] NVARCHAR(200) NULL,
    [CustomerAccountNumberSnapshot] NVARCHAR(50) NULL,
    [CustomerNameSnapshot] NVARCHAR(120) NULL,
    [CustomerEmailSnapshot] NVARCHAR(320) NULL,
    [CustomerPhoneSnapshot] NVARCHAR(50) NULL,
    [ShippingLabelSnapshot] NVARCHAR(120) NULL,
    [ShippingLine1Snapshot] NVARCHAR(200) NULL,
    [ShippingLine2Snapshot] NVARCHAR(200) NULL,
    [ShippingCitySnapshot] NVARCHAR(120) NULL,
    [ShippingStateSnapshot] NVARCHAR(120) NULL,
    [ShippingPostalCodeSnapshot] NVARCHAR(32) NULL,
    [ShippingCountrySnapshot] NCHAR(2) NULL,
    [ShippingAddressTypeCodeSnapshot] NVARCHAR(32) NULL,
    [CreatedByUserId] INT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    [ModifiedByUserId] INT NULL,
    [ModificationDate] DATETIMEOFFSET(7) NULL,
    [DeletionDate] DATETIMEOFFSET(7) NULL,
    [DeletedByUserId] INT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_SalesOrder] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_SalesOrder_OrderNumber] UNIQUE ([OrderNumber]),
    CONSTRAINT [FK_SalesOrder_Quote] FOREIGN KEY ([SourceQuoteId]) REFERENCES [dbo].[Quote] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrder_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrder_CustomerAddress] FOREIGN KEY ([CustomerId], [ShippingAddressId]) REFERENCES [dbo].[CustomerAddress] ([CustomerId], [Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrder_SalesOrderStatus] FOREIGN KEY ([StatusId]) REFERENCES [dbo].[SalesOrderStatus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrder_Carrier] FOREIGN KEY ([CarrierId]) REFERENCES [dbo].[Carrier] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrder_ApplicationUser_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrder_ApplicationUser_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrder_ApplicationUser_DeletedByUserId] FOREIGN KEY ([DeletedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_SalesOrder_OrderNumber_NotBlank] CHECK (LEN(LTRIM(RTRIM([OrderNumber]))) > 0),
    CONSTRAINT [CK_SalesOrder_TrackingNumber_NotBlank] CHECK ([TrackingNumber] IS NULL OR LEN(LTRIM(RTRIM([TrackingNumber]))) > 0),
    CONSTRAINT [CK_SalesOrder_ShippedRequirements] CHECK ([StatusId] NOT IN (4, 5) OR ([CarrierId] IS NOT NULL AND [TrackingNumber] IS NOT NULL)),
    CONSTRAINT [CK_SalesOrder_DeletionPair] CHECK (([DeletionDate] IS NULL AND [DeletedByUserId] IS NULL) OR ([DeletionDate] IS NOT NULL AND [DeletedByUserId] IS NOT NULL))
);
GO
CREATE UNIQUE INDEX [UQ_SalesOrder_SourceQuoteId] ON [dbo].[SalesOrder] ([SourceQuoteId]) WHERE [SourceQuoteId] IS NOT NULL;
GO
CREATE INDEX [IX_SalesOrder_StatusId_Id] ON [dbo].[SalesOrder] ([StatusId], [Id]);
GO
CREATE INDEX [IX_SalesOrder_CustomerId_Id] ON [dbo].[SalesOrder] ([CustomerId], [Id]);
GO
CREATE INDEX [IX_SalesOrder_RequestedShipDate_Id] ON [dbo].[SalesOrder] ([RequestedShipDate], [Id]);
GO
CREATE INDEX [IX_SalesOrder_TrackingNumber] ON [dbo].[SalesOrder] ([TrackingNumber]) WHERE [TrackingNumber] IS NOT NULL;
