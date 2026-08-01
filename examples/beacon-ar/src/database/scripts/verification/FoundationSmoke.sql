SET NOCOUNT ON;

IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NULL THROW 51000, 'Product table is missing.', 1;
IF OBJECT_ID(N'[dbo].[QuotePricing]', N'V') IS NULL THROW 51000, 'QuotePricing view is missing.', 1;
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
