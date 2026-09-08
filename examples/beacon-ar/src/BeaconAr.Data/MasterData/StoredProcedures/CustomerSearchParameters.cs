namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class CustomerSearchParameters
{
    #region Properties

    public string? Search { get; set; }

    public bool? Active { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public string SortField { get; set; } = "id";

    public string SortDirection { get; set; } = "asc";

    #endregion
}
