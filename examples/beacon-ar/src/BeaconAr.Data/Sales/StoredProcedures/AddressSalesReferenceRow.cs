namespace BeaconAr.Data.Sales.StoredProcedures;
internal sealed class AddressSalesReferenceRow
{
    #region Properties

    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string AddressTypeCode { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Line1 { get; set; } = null!;
    public string? Line2 { get; set; }
    public string City { get; set; } = null!;
    public string? State { get; set; }
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;

    #endregion
}
