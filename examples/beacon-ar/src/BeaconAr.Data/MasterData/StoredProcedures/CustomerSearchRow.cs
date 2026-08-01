namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class CustomerSearchRow
{
    #region Properties

    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal CreditLimit { get; set; }
    public short PaymentTermsDays { get; set; }
    public bool IsActive { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTimeOffset? ModificationDate { get; set; }
    public byte[] RowVersion { get; set; } = [];

    #endregion
}
