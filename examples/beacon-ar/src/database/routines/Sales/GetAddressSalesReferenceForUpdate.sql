CREATE PROCEDURE [dbo].[GetAddressSalesReferenceForUpdate]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.[Id], a.[CustomerId], t.[Code] AS [AddressTypeCode], a.[Label], a.[Line1], a.[Line2],
           a.[City], a.[State], a.[PostalCode], a.[Country]
    FROM [dbo].[CustomerAddress] AS a WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN [dbo].[AddressType] AS t ON t.[Id] = a.[AddressTypeId]
    WHERE a.[Id] = @Id;
END;
