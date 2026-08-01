CREATE PROCEDURE [dbo].[SearchSalesOrder]
    @Search NVARCHAR(320) = NULL,
    @StatusId INT = NULL,
    @CustomerId INT = NULL,
    @SourceQuoteId INT = NULL,
    @PageNumber INT,
    @PageSize INT,
    @SortField NVARCHAR(32),
    @SortDirection NVARCHAR(4)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    IF @PageNumber < 1 OR @PageSize < 1 OR @PageSize > 100
        THROW 50001, 'Invalid paging parameters.', 1;
    IF @SortField NOT IN (N'orderNumber', N'status', N'requestedShipDate')
        THROW 50002, 'Invalid sales order sort field.', 1;
    IF @SortDirection NOT IN (N'asc', N'desc')
        THROW 50003, 'Invalid sort direction.', 1;

    DECLARE @SearchPattern NVARCHAR(640) = REPLACE(REPLACE(REPLACE(REPLACE(@Search, N'\', N'\\'), N'[', N'\['), N'%', N'\%'), N'_', N'\_');

    BEGIN TRY
    BEGIN TRANSACTION;

    SELECT COUNT(1)
    FROM [dbo].[SalesOrder] AS [O]
    WHERE [O].[DeletionDate] IS NULL
      AND (@Search IS NULL OR [O].[OrderNumber] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [O].[TrackingNumber] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [O].[CustomerAccountNumberSnapshot] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [O].[CustomerNameSnapshot] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\')
      AND (@StatusId IS NULL OR [O].[StatusId] = @StatusId)
      AND (@CustomerId IS NULL OR [O].[CustomerId] = @CustomerId)
      AND (@SourceQuoteId IS NULL OR [O].[SourceQuoteId] = @SourceQuoteId);

    SELECT [O].[Id], [O].[OrderNumber], [O].[SourceQuoteId], [O].[CustomerId],
           [O].[CustomerAccountNumberSnapshot], [O].[CustomerNameSnapshot], [O].[StatusId],
           [O].[RequestedShipDate], [O].[CarrierId], [C].[Name] AS [CarrierName], [O].[TrackingNumber],
           [P].[Subtotal], [P].[DiscountTotal], [P].[GrandTotal], [O].[CreatedByUserId], [O].[CreationDate],
           [O].[ModifiedByUserId], [O].[ModificationDate], [O].[RowVersion]
    FROM [dbo].[SalesOrder] AS [O]
    INNER JOIN [dbo].[SalesOrderPricing] AS [P] ON [P].[SalesOrderId] = [O].[Id]
    LEFT JOIN [dbo].[Carrier] AS [C] ON [C].[Id] = [O].[CarrierId]
    WHERE [O].[DeletionDate] IS NULL
      AND (@Search IS NULL OR [O].[OrderNumber] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [O].[TrackingNumber] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [O].[CustomerAccountNumberSnapshot] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [O].[CustomerNameSnapshot] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\')
      AND (@StatusId IS NULL OR [O].[StatusId] = @StatusId)
      AND (@CustomerId IS NULL OR [O].[CustomerId] = @CustomerId)
      AND (@SourceQuoteId IS NULL OR [O].[SourceQuoteId] = @SourceQuoteId)
    ORDER BY
        CASE WHEN @SortField = N'orderNumber' AND @SortDirection = N'asc' THEN [O].[OrderNumber] END ASC,
        CASE WHEN @SortField = N'orderNumber' AND @SortDirection = N'desc' THEN [O].[OrderNumber] END DESC,
        CASE WHEN @SortField = N'status' AND @SortDirection = N'asc' THEN [O].[StatusId] END ASC,
        CASE WHEN @SortField = N'status' AND @SortDirection = N'desc' THEN [O].[StatusId] END DESC,
        CASE WHEN @SortField = N'requestedShipDate' AND @SortDirection = N'asc' THEN [O].[RequestedShipDate] END ASC,
        CASE WHEN @SortField = N'requestedShipDate' AND @SortDirection = N'desc' THEN [O].[RequestedShipDate] END DESC,
        CASE WHEN @SortDirection = N'asc' THEN [O].[Id] END ASC,
        [O].[Id] DESC
    OFFSET ((CONVERT(BIGINT, @PageNumber) - 1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY;

    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
