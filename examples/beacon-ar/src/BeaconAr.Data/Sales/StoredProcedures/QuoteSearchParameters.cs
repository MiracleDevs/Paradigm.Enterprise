namespace BeaconAr.Data.Sales.StoredProcedures;

internal sealed class QuoteSearchParameters
{
    #region Properties

    public string? Search { get; set; }
    public int? StatusId { get; set; }
    public int? CustomerId { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortField { get; set; } = "quoteNumber";
    public string SortDirection { get; set; } = "desc";

    #endregion
}
