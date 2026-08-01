CREATE TABLE [dbo].[Quote]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [QuoteNumber] NVARCHAR(20) NOT NULL,
    [CustomerId] INT NOT NULL,
    [ShippingAddressId] INT NOT NULL,
    [QuoteDate] DATE NOT NULL,
    [ValidUntil] DATE NOT NULL,
    [StatusId] INT NOT NULL,
    [Notes] NVARCHAR(1000) NULL,
    [CustomerAccountNumberSnapshot] NVARCHAR(50) NOT NULL,
    [CustomerNameSnapshot] NVARCHAR(120) NOT NULL,
    [CustomerEmailSnapshot] NVARCHAR(320) NOT NULL,
    [CustomerPhoneSnapshot] NVARCHAR(50) NULL,
    [ShippingLabelSnapshot] NVARCHAR(120) NOT NULL,
    [ShippingLine1Snapshot] NVARCHAR(200) NOT NULL,
    [ShippingLine2Snapshot] NVARCHAR(200) NULL,
    [ShippingCitySnapshot] NVARCHAR(120) NOT NULL,
    [ShippingStateSnapshot] NVARCHAR(120) NULL,
    [ShippingPostalCodeSnapshot] NVARCHAR(32) NOT NULL,
    [ShippingCountrySnapshot] NCHAR(2) NOT NULL,
    [ShippingAddressTypeCodeSnapshot] NVARCHAR(32) NOT NULL,
    [CreatedByUserId] INT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    [ModifiedByUserId] INT NULL,
    [ModificationDate] DATETIMEOFFSET(7) NULL,
    [DeletionDate] DATETIMEOFFSET(7) NULL,
    [DeletedByUserId] INT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_Quote] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Quote_QuoteNumber] UNIQUE ([QuoteNumber]),
    CONSTRAINT [FK_Quote_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Quote_CustomerAddress] FOREIGN KEY ([CustomerId], [ShippingAddressId]) REFERENCES [dbo].[CustomerAddress] ([CustomerId], [Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Quote_QuoteStatus] FOREIGN KEY ([StatusId]) REFERENCES [dbo].[QuoteStatus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Quote_ApplicationUser_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Quote_ApplicationUser_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Quote_ApplicationUser_DeletedByUserId] FOREIGN KEY ([DeletedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Quote_QuoteNumber_NotBlank] CHECK (LEN(LTRIM(RTRIM([QuoteNumber]))) > 0),
    CONSTRAINT [CK_Quote_ValidUntil] CHECK ([ValidUntil] >= [QuoteDate]),
    CONSTRAINT [CK_Quote_DeletionPair] CHECK (([DeletionDate] IS NULL AND [DeletedByUserId] IS NULL) OR ([DeletionDate] IS NOT NULL AND [DeletedByUserId] IS NOT NULL))
);
GO
CREATE INDEX [IX_Quote_StatusId_QuoteDate_Id] ON [dbo].[Quote] ([StatusId], [QuoteDate], [Id]);
GO
CREATE INDEX [IX_Quote_CustomerId_Id] ON [dbo].[Quote] ([CustomerId], [Id]);
GO
CREATE INDEX [IX_Quote_ValidUntil_Id] ON [dbo].[Quote] ([ValidUntil], [Id]);
