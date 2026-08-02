CREATE VIEW [dbo].[QuoteView]
WITH SCHEMABINDING
AS
    SELECT
        [Quote].[Id],
        [Quote].[QuoteNumber],
        [Quote].[CustomerId],
        [Customer].[AccountNumber] AS [CustomerAccountNumber],
        [Customer].[Name] AS [CustomerName],
        [Customer].[IsActive] AS [CustomerIsActive],
        [Quote].[ShippingAddressId],
        [ShippingAddress].[Label] AS [ShippingAddressLabel],
        [ShippingAddress].[Line1] AS [ShippingAddressLine1],
        [ShippingAddress].[Line2] AS [ShippingAddressLine2],
        [ShippingAddress].[City] AS [ShippingAddressCity],
        [ShippingAddress].[State] AS [ShippingAddressState],
        [ShippingAddress].[PostalCode] AS [ShippingAddressPostalCode],
        [ShippingAddress].[Country] AS [ShippingAddressCountry],
        [AddressType].[Code] AS [ShippingAddressTypeCode],
        [AddressType].[DisplayName] AS [ShippingAddressTypeDisplayName],
        [Quote].[QuoteDate],
        [Quote].[ValidUntil],
        [Quote].[StatusId],
        [Status].[Code] AS [StatusCode],
        [Status].[DisplayName] AS [StatusDisplayName],
        [Quote].[Notes],
        [Quote].[CustomerAccountNumberSnapshot],
        [Quote].[CustomerNameSnapshot],
        [Quote].[CustomerEmailSnapshot],
        [Quote].[CustomerPhoneSnapshot],
        [Quote].[ShippingLabelSnapshot],
        [Quote].[ShippingLine1Snapshot],
        [Quote].[ShippingLine2Snapshot],
        [Quote].[ShippingCitySnapshot],
        [Quote].[ShippingStateSnapshot],
        [Quote].[ShippingPostalCodeSnapshot],
        [Quote].[ShippingCountrySnapshot],
        [Quote].[ShippingAddressTypeCodeSnapshot],
        [Pricing].[Subtotal],
        [Pricing].[DiscountTotal],
        [Pricing].[GrandTotal],
        [SalesOrder].[Id] AS [SalesOrderId],
        [Quote].[CreatedByUserId],
        [CreatedByUser].[DisplayName] AS [CreatedByUserDisplayName],
        [Quote].[CreationDate],
        [Quote].[ModifiedByUserId],
        [ModifiedByUser].[DisplayName] AS [ModifiedByUserDisplayName],
        [Quote].[ModificationDate],
        [Quote].[DeletionDate],
        [Quote].[DeletedByUserId],
        [DeletedByUser].[DisplayName] AS [DeletedByUserDisplayName],
        [Quote].[RowVersion]
    FROM [dbo].[Quote] AS [Quote]
    INNER JOIN [dbo].[Customer] AS [Customer] ON [Customer].[Id] = [Quote].[CustomerId]
    INNER JOIN [dbo].[CustomerAddress] AS [ShippingAddress] ON [ShippingAddress].[Id] = [Quote].[ShippingAddressId]
    INNER JOIN [dbo].[AddressType] AS [AddressType] ON [AddressType].[Id] = [ShippingAddress].[AddressTypeId]
    INNER JOIN [dbo].[QuoteStatus] AS [Status] ON [Status].[Id] = [Quote].[StatusId]
    INNER JOIN [dbo].[QuotePricing] AS [Pricing] ON [Pricing].[QuoteId] = [Quote].[Id]
    LEFT JOIN [dbo].[SalesOrder] AS [SalesOrder] ON [SalesOrder].[SourceQuoteId] = [Quote].[Id]
    LEFT JOIN [dbo].[ApplicationUser] AS [CreatedByUser] ON [CreatedByUser].[Id] = [Quote].[CreatedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [ModifiedByUser] ON [ModifiedByUser].[Id] = [Quote].[ModifiedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [DeletedByUser] ON [DeletedByUser].[Id] = [Quote].[DeletedByUserId];
