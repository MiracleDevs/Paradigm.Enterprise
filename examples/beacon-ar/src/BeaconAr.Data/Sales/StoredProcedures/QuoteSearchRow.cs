namespace BeaconAr.Data.Sales.StoredProcedures;

internal sealed class QuoteSearchRow
{
    #region Properties

    public int Id { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerAccountNumberSnapshot { get; set; } = string.Empty;
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public DateTime QuoteDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public int StatusId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public int? SalesOrderId { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTimeOffset? ModificationDate { get; set; }
    public byte[] RowVersion { get; set; } = [];

    #endregion
}
