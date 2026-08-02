SET NOCOUNT ON;

IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL THROW 51000, 'Product table is missing.', 1;
IF OBJECT_ID(N'[dbo].[ApplicationUserView]', N'V') IS NULL THROW 51000, 'ApplicationUserView is missing.', 1;
IF OBJECT_ID(N'[dbo].[ProductView]', N'V') IS NULL THROW 51000, 'ProductView is missing.', 1;
IF OBJECT_ID(N'[dbo].[CustomerView]', N'V') IS NULL THROW 51000, 'CustomerView is missing.', 1;
IF OBJECT_ID(N'[dbo].[CustomerAddressView]', N'V') IS NULL THROW 51000, 'CustomerAddressView is missing.', 1;
IF OBJECT_ID(N'[dbo].[CarrierView]', N'V') IS NULL THROW 51000, 'CarrierView is missing.', 1;
IF OBJECT_ID(N'[dbo].[QuoteView]', N'V') IS NULL THROW 51000, 'QuoteView is missing.', 1;
IF OBJECT_ID(N'[dbo].[QuoteLineView]', N'V') IS NULL THROW 51000, 'QuoteLineView is missing.', 1;
IF OBJECT_ID(N'[dbo].[SalesOrderView]', N'V') IS NULL THROW 51000, 'SalesOrderView is missing.', 1;
IF OBJECT_ID(N'[dbo].[SalesOrderLineView]', N'V') IS NULL THROW 51000, 'SalesOrderLineView is missing.', 1;
IF EXISTS
(
    SELECT 1
    FROM
    (
        VALUES
            (N'ApplicationUserView'),
            (N'ProductView'),
            (N'CustomerView'),
            (N'CustomerAddressView'),
            (N'CarrierView'),
            (N'QuoteView'),
            (N'QuoteLineView'),
            (N'SalesOrderView'),
            (N'SalesOrderLineView')
    ) AS [ExpectedView] ([Name])
    INNER JOIN [sys].[views] AS [View] ON [View].[name] = [ExpectedView].[Name]
    INNER JOIN [sys].[schemas] AS [Schema] ON [Schema].[schema_id] = [View].[schema_id]
    WHERE [Schema].[name] = N'dbo'
      AND OBJECTPROPERTY([View].[object_id], N'IsSchemaBound') <> 1
)
THROW 51000, 'An entity view is not schema-bound.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[ApplicationUserView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'ApplicationUserView duplicates base rows.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[ProductView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'ProductView duplicates base rows.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[CustomerView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'CustomerView duplicates base rows.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[CustomerAddressView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'CustomerAddressView duplicates base rows.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[CarrierView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'CarrierView duplicates base rows.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[QuoteView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'QuoteView duplicates base rows.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[QuoteLineView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'QuoteLineView duplicates base rows.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[SalesOrderView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'SalesOrderView duplicates base rows.', 1;
IF EXISTS (SELECT [Id] FROM [dbo].[SalesOrderLineView] GROUP BY [Id] HAVING COUNT_BIG(*) > 1) THROW 51000, 'SalesOrderLineView duplicates base rows.', 1;
IF (SELECT COUNT(*) FROM [dbo].[AddressType]) <> 3 THROW 51000, 'AddressType seed mismatch.', 1;
IF (SELECT COUNT(*) FROM [dbo].[IdempotencyState]) <> 3 THROW 51000, 'IdempotencyState seed mismatch.', 1;
IF (SELECT COUNT(*) FROM [dbo].[QuoteStatus]) <> 5 THROW 51000, 'QuoteStatus seed mismatch.', 1;
IF (SELECT COUNT(*) FROM [dbo].[SalesOrderStatus]) <> 6 THROW 51000, 'SalesOrderStatus seed mismatch.', 1;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action <> 0) THROW 51000, 'Unexpected cascading foreign key.', 1;
IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys AS foreignKey
    INNER JOIN sys.foreign_key_columns AS foreignKeyColumn ON foreignKeyColumn.constraint_object_id = foreignKey.object_id
    WHERE foreignKey.name = N'FK_Quote_CustomerAddress'
    GROUP BY foreignKey.object_id
    HAVING COUNT(*) = 2
)
THROW 51000, 'Quote shipping-address ownership constraint is missing.', 1;
IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationUser]', N'U')
      AND name IN (N'Issuer', N'Subject')
      AND collation_name <> N'Latin1_General_100_BIN2'
)
THROW 51000, 'OIDC identifiers must use exact binary collation.', 1;
