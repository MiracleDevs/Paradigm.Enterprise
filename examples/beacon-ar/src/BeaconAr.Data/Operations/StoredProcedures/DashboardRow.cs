namespace BeaconAr.Data.Operations.StoredProcedures;

internal sealed class DashboardRow
{
    #region Properties

    public long Products { get; set; }
    public long Customers { get; set; }
    public long Carriers { get; set; }
    public long OpenQuotes { get; set; }
    public long ActiveOrders { get; set; }
    public DateTimeOffset AsOf { get; set; }

    #endregion
}
