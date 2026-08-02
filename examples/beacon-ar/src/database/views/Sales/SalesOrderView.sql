CREATE VIEW [dbo].[SalesOrderView]
WITH SCHEMABINDING
AS
    SELECT
        [SalesOrder].[Id],
        [SalesOrder].[OrderNumber],
        [SalesOrder].[SourceQuoteId],
        [SourceQuote].[QuoteNumber] AS [SourceQuoteNumber],
        [SalesOrder].[CustomerId],
        [Customer].[AccountNumber] AS [CustomerAccountNumber],
        [Customer].[Name] AS [CustomerName],
        [Customer].[IsActive] AS [CustomerIsActive],
        [SalesOrder].[ShippingAddressId],
        [ShippingAddress].[Label] AS [ShippingAddressLabel],
        [ShippingAddress].[Line1] AS [ShippingAddressLine1],
        [ShippingAddress].[Line2] AS [ShippingAddressLine2],
        [ShippingAddress].[City] AS [ShippingAddressCity],
        [ShippingAddress].[State] AS [ShippingAddressState],
        [ShippingAddress].[PostalCode] AS [ShippingAddressPostalCode],
        [ShippingAddress].[Country] AS [ShippingAddressCountry],
        [AddressType].[Code] AS [ShippingAddressTypeCode],
        [AddressType].[DisplayName] AS [ShippingAddressTypeDisplayName],
        [SalesOrder].[StatusId],
        [Status].[Code] AS [StatusCode],
        [Status].[DisplayName] AS [StatusDisplayName],
        [SalesOrder].[RequestedShipDate],
        [SalesOrder].[CarrierId],
        [Carrier].[Code] AS [CarrierCode],
        [Carrier].[Name] AS [CarrierName],
        [Carrier].[ServiceLevel] AS [CarrierServiceLevel],
        [SalesOrder].[TrackingNumber],
        [SalesOrder].[CustomerAccountNumberSnapshot],
        [SalesOrder].[CustomerNameSnapshot],
        [SalesOrder].[CustomerEmailSnapshot],
        [SalesOrder].[CustomerPhoneSnapshot],
        [SalesOrder].[ShippingLabelSnapshot],
        [SalesOrder].[ShippingLine1Snapshot],
        [SalesOrder].[ShippingLine2Snapshot],
        [SalesOrder].[ShippingCitySnapshot],
        [SalesOrder].[ShippingStateSnapshot],
        [SalesOrder].[ShippingPostalCodeSnapshot],
        [SalesOrder].[ShippingCountrySnapshot],
        [SalesOrder].[ShippingAddressTypeCodeSnapshot],
        [Pricing].[Subtotal],
        [Pricing].[DiscountTotal],
        [Pricing].[GrandTotal],
        [SalesOrder].[CreatedByUserId],
        [CreatedByUser].[DisplayName] AS [CreatedByUserDisplayName],
        [SalesOrder].[CreationDate],
        [SalesOrder].[ModifiedByUserId],
        [ModifiedByUser].[DisplayName] AS [ModifiedByUserDisplayName],
        [SalesOrder].[ModificationDate],
        [SalesOrder].[DeletionDate],
        [SalesOrder].[DeletedByUserId],
        [DeletedByUser].[DisplayName] AS [DeletedByUserDisplayName],
        [SalesOrder].[RowVersion]
    FROM [dbo].[SalesOrder] AS [SalesOrder]
    LEFT JOIN [dbo].[Quote] AS [SourceQuote] ON [SourceQuote].[Id] = [SalesOrder].[SourceQuoteId]
    INNER JOIN [dbo].[Customer] AS [Customer] ON [Customer].[Id] = [SalesOrder].[CustomerId]
    INNER JOIN [dbo].[CustomerAddress] AS [ShippingAddress] ON [ShippingAddress].[Id] = [SalesOrder].[ShippingAddressId]
    INNER JOIN [dbo].[AddressType] AS [AddressType] ON [AddressType].[Id] = [ShippingAddress].[AddressTypeId]
    INNER JOIN [dbo].[SalesOrderStatus] AS [Status] ON [Status].[Id] = [SalesOrder].[StatusId]
    LEFT JOIN [dbo].[Carrier] AS [Carrier] ON [Carrier].[Id] = [SalesOrder].[CarrierId]
    INNER JOIN [dbo].[SalesOrderPricing] AS [Pricing] ON [Pricing].[SalesOrderId] = [SalesOrder].[Id]
    LEFT JOIN [dbo].[ApplicationUser] AS [CreatedByUser] ON [CreatedByUser].[Id] = [SalesOrder].[CreatedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [ModifiedByUser] ON [ModifiedByUser].[Id] = [SalesOrder].[ModifiedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [DeletedByUser] ON [DeletedByUser].[Id] = [SalesOrder].[DeletedByUserId];
