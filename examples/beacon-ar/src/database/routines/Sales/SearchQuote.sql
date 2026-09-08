CREATE PROCEDURE [dbo].[SearchQuote]
    @Search NVARCHAR(320) = NULL,
    @StatusId INT = NULL,
    @CustomerId INT = NULL,
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
    IF @SortField NOT IN (N'quoteNumber', N'quoteDate', N'validUntil', N'status')
        THROW 50002, 'Invalid quote sort field.', 1;
    IF @SortDirection NOT IN (N'asc', N'desc')
        THROW 50003, 'Invalid sort direction.', 1;

    DECLARE @SearchPattern NVARCHAR(640) = REPLACE(REPLACE(REPLACE(REPLACE(@Search, N'\', N'\\'), N'[', N'\['), N'%', N'\%'), N'_', N'\_');

    BEGIN TRY
    BEGIN TRANSACTION;

    SELECT COUNT(1)
    FROM [dbo].[QuoteView] AS [Q]
    WHERE [Q].[DeletionDate] IS NULL
      AND (@Search IS NULL OR [Q].[QuoteNumber] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [Q].[CustomerAccountNumberSnapshot] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [Q].[CustomerNameSnapshot] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\')
      AND (@StatusId IS NULL OR [Q].[StatusId] = @StatusId)
      AND (@CustomerId IS NULL OR [Q].[CustomerId] = @CustomerId);

    SELECT [Q].*
    FROM [dbo].[QuoteView] AS [Q]
    WHERE [Q].[DeletionDate] IS NULL
      AND (@Search IS NULL OR [Q].[QuoteNumber] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [Q].[CustomerAccountNumberSnapshot] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\'
          OR [Q].[CustomerNameSnapshot] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\')
      AND (@StatusId IS NULL OR [Q].[StatusId] = @StatusId)
      AND (@CustomerId IS NULL OR [Q].[CustomerId] = @CustomerId)
    ORDER BY
        CASE WHEN @SortField = N'quoteNumber' AND @SortDirection = N'asc' THEN [Q].[QuoteNumber] END ASC,
        CASE WHEN @SortField = N'quoteNumber' AND @SortDirection = N'desc' THEN [Q].[QuoteNumber] END DESC,
        CASE WHEN @SortField = N'quoteDate' AND @SortDirection = N'asc' THEN [Q].[QuoteDate] END ASC,
        CASE WHEN @SortField = N'quoteDate' AND @SortDirection = N'desc' THEN [Q].[QuoteDate] END DESC,
        CASE WHEN @SortField = N'validUntil' AND @SortDirection = N'asc' THEN [Q].[ValidUntil] END ASC,
        CASE WHEN @SortField = N'validUntil' AND @SortDirection = N'desc' THEN [Q].[ValidUntil] END DESC,
        CASE WHEN @SortField = N'status' AND @SortDirection = N'asc' THEN [Q].[StatusId] END ASC,
        CASE WHEN @SortField = N'status' AND @SortDirection = N'desc' THEN [Q].[StatusId] END DESC,
        CASE WHEN @SortDirection = N'asc' THEN [Q].[Id] END ASC,
        [Q].[Id] DESC
    OFFSET ((CONVERT(BIGINT, @PageNumber) - 1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY;

    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
