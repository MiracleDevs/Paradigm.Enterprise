namespace BeaconAr.Data.MasterData.StoredProcedures;

internal sealed class AddressSearchRow
{
    #region Properties

    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Line1 { get; set; } = string.Empty;

    public string? Line2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string? State { get; set; }

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public bool DefaultBilling { get; set; }

    public bool DefaultShipping { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTimeOffset CreationDate { get; set; }

    public int? ModifiedByUserId { get; set; }

    public DateTimeOffset? ModificationDate { get; set; }

    public byte[] RowVersion { get; set; } = [];

    #endregion
}
