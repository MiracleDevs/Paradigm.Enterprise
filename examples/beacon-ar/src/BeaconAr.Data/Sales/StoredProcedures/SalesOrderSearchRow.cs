namespace BeaconAr.Data.Sales.StoredProcedures;

internal sealed class SalesOrderSearchRow
{
    #region Properties

    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int? SourceQuoteId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerAccountNumberSnapshot { get; set; } = string.Empty;
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime? RequestedShipDate { get; set; }
    public int? CarrierId { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTimeOffset? ModificationDate { get; set; }
    public byte[] RowVersion { get; set; } = [];

    #endregion
}
