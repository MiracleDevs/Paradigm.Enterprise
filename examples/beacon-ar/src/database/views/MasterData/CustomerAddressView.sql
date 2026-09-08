CREATE VIEW [dbo].[CustomerAddressView]
WITH SCHEMABINDING
AS
    SELECT
        [Address].[Id],
        [Address].[CustomerId],
        [Customer].[AccountNumber] AS [CustomerAccountNumber],
        [Customer].[Name] AS [CustomerName],
        [Address].[AddressTypeId],
        [AddressType].[Code] AS [AddressTypeCode],
        [AddressType].[DisplayName] AS [AddressTypeDisplayName],
        [Address].[Label],
        [Address].[Line1],
        [Address].[Line2],
        [Address].[City],
        [Address].[State],
        [Address].[PostalCode],
        [Address].[Country],
        [Address].[IsDefaultBilling],
        [Address].[IsDefaultShipping],
        [Address].[CreatedByUserId],
        [CreatedByUser].[DisplayName] AS [CreatedByUserDisplayName],
        [Address].[CreationDate],
        [Address].[ModifiedByUserId],
        [ModifiedByUser].[DisplayName] AS [ModifiedByUserDisplayName],
        [Address].[ModificationDate],
        [Address].[RowVersion]
    FROM [dbo].[CustomerAddress] AS [Address]
    INNER JOIN [dbo].[Customer] AS [Customer] ON [Customer].[Id] = [Address].[CustomerId]
    INNER JOIN [dbo].[AddressType] AS [AddressType] ON [AddressType].[Id] = [Address].[AddressTypeId]
    LEFT JOIN [dbo].[ApplicationUser] AS [CreatedByUser] ON [CreatedByUser].[Id] = [Address].[CreatedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [ModifiedByUser] ON [ModifiedByUser].[Id] = [Address].[ModifiedByUserId];
