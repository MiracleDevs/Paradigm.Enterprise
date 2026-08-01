namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class AddressSearchParameters
{
    #region Properties

    public string? Search { get; set; }

    public int? CustomerId { get; set; }

    public string? Type { get; set; }

    public string? Usage { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public string SortField { get; set; } = "id";

    public string SortDirection { get; set; } = "asc";

    #endregion
}
