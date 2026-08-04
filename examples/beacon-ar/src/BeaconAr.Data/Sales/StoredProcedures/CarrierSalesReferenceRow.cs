namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class CarrierSalesReferenceRow
{
    #region Properties

    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }

    #endregion
}
