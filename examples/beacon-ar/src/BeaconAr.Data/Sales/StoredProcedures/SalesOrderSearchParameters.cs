namespace BeaconAr.Data.Sales.StoredProcedures;

internal sealed class SalesOrderSearchParameters
{
    #region Properties

    public string? Search { get; set; }
    public int? StatusId { get; set; }
    public int? CustomerId { get; set; }
    public int? SourceQuoteId { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortField { get; set; } = "orderNumber";
    public string SortDirection { get; set; } = "desc";

    #endregion
}
