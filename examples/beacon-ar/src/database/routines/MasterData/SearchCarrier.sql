CREATE PROCEDURE [dbo].[SearchCarrier]
    @Search NVARCHAR(320) = NULL,
    @Active BIT = NULL,
    @PageNumber INT,
    @PageSize INT,
    @SortField NVARCHAR(16),
    @SortDirection NVARCHAR(4)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SearchPattern NVARCHAR(640) = REPLACE(REPLACE(REPLACE(REPLACE(@Search, N'\', N'\\'), N'[', N'\['), N'%', N'\%'), N'_', N'\_');

    SELECT COUNT(1)
    FROM [dbo].[Carrier]
    WHERE (@Search IS NULL OR [Code] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Name] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [ServiceLevel] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\')
      AND (@Active IS NULL OR [IsActive] = @Active);

    SELECT [Id], [Code], [Name], [ServiceLevel], [TrackingUrlTemplate], [IsActive], [CreatedByUserId],
           [CreationDate], [ModifiedByUserId], [ModificationDate], [RowVersion]
    FROM [dbo].[Carrier]
    WHERE (@Search IS NULL OR [Code] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [Name] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\' OR [ServiceLevel] LIKE N'%' + @SearchPattern + N'%' ESCAPE N'\')
      AND (@Active IS NULL OR [IsActive] = @Active)
    ORDER BY
        CASE WHEN @SortField = N'name' AND @SortDirection = N'asc' THEN [Name] END ASC,
        CASE WHEN @SortField = N'name' AND @SortDirection = N'desc' THEN [Name] END DESC,
        CASE WHEN @SortField = N'id' AND @SortDirection = N'desc' THEN [Id] END DESC,
        [Id] ASC
    OFFSET ((CONVERT(BIGINT, @PageNumber) - 1) * @PageSize) ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
