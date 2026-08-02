CREATE PROCEDURE [dbo].[SearchAddress]
    @Search NVARCHAR(320) = NULL,
    @CustomerId INT = NULL,
    @Type NVARCHAR(32) = NULL,
    @Usage NVARCHAR(16) = NULL,
    @PageNumber INT,
    @PageSize INT,
    @SortField NVARCHAR(16),
    @SortDirection NVARCHAR(4)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SearchPattern NVARCHAR(640) = REPLACE(REPLACE(REPLACE(REPLACE(@Search, N'\', N'\\'), N'[', N'\['), N'%', N'\%'), N'_', N'\_');

    SELECT COUNT(1)
    FROM [dbo].[CustomerAddressView] AS [Address]
    WHERE (@Search IS NULL OR [Address].[Label] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Address].[Line1] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR
           [Address].[Line2] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Address].[City] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR
           [Address].[State] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Address].[PostalCode] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Address].[Country] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\')
      AND (@CustomerId IS NULL OR [Address].[CustomerId] = @CustomerId)
      AND (@Type IS NULL OR [Address].[AddressTypeCode] = @Type)
      AND (@Usage IS NULL OR @Usage = N'billing' AND [Address].[AddressTypeCode] IN (N'billing', N'both') OR @Usage = N'shipping' AND [Address].[AddressTypeCode] IN (N'shipping', N'both'));

    SELECT [Address].[Id], [Address].[CustomerId], [Address].[CustomerAccountNumber], [Address].[CustomerName],
           [Address].[AddressTypeId], [Address].[AddressTypeCode], [Address].[AddressTypeDisplayName],
           [Address].[Label], [Address].[Line1], [Address].[Line2], [Address].[City], [Address].[State],
           [Address].[PostalCode], RTRIM([Address].[Country]) AS [Country], [Address].[IsDefaultBilling],
           [Address].[IsDefaultShipping], [Address].[CreatedByUserId], [Address].[CreatedByUserDisplayName],
           [Address].[CreationDate], [Address].[ModifiedByUserId], [Address].[ModifiedByUserDisplayName],
           [Address].[ModificationDate], [Address].[RowVersion]
    FROM [dbo].[CustomerAddressView] AS [Address]
    WHERE (@Search IS NULL OR [Address].[Label] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Address].[Line1] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR
           [Address].[Line2] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Address].[City] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR
           [Address].[State] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Address].[PostalCode] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Address].[Country] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\')
      AND (@CustomerId IS NULL OR [Address].[CustomerId] = @CustomerId)
      AND (@Type IS NULL OR [Address].[AddressTypeCode] = @Type)
      AND (@Usage IS NULL OR @Usage = N'billing' AND [Address].[AddressTypeCode] IN (N'billing', N'both') OR @Usage = N'shipping' AND [Address].[AddressTypeCode] IN (N'shipping', N'both'))
    ORDER BY
        CASE WHEN @SortField = N'name' AND @SortDirection = N'asc' THEN [Address].[Label] END ASC,
        CASE WHEN @SortField = N'name' AND @SortDirection = N'desc' THEN [Address].[Label] END DESC,
        CASE WHEN @SortField = N'id' AND @SortDirection = N'desc' THEN [Address].[Id] END DESC,
        [Address].[Id] ASC
    OFFSET ((CONVERT(BIGINT, @PageNumber) - 1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
