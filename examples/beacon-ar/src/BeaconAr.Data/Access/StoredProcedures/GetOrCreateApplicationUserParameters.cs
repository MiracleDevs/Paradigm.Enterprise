namespace BeaconAr.Data.Access.StoredProcedures;

internal sealed class GetOrCreateApplicationUserParameters
{
    #region Properties

    public string Issuer { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Email { get; set; }
    public DateTimeOffset Now { get; set; }

    #endregion
}
