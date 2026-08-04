namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class CustomerSalesReferenceRow
{
    #region Properties

    public int Id { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }

    #endregion
}
