namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class CarrierSearchRow
{
    #region Properties

    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ServiceLevel { get; set; } = string.Empty;
    public string? TrackingUrlTemplate { get; set; }
    public bool IsActive { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public int? ModifiedByUserId { get; set; }
    public DateTimeOffset? ModificationDate { get; set; }
    public byte[] RowVersion { get; set; } = [];

    #endregion
}
