namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class ProductSalesReferenceRow
{
    #region Properties

    public int Id { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }

    #endregion
}
