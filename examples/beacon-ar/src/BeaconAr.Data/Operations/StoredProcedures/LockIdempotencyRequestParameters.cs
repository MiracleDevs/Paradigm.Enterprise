namespace BeaconAr.Data.Operations.StoredProcedures;

internal sealed class LockIdempotencyRequestParameters
{
    #region Properties

    public int UserId { get; set; }
    public string Operation { get; set; } = null!;
    public byte[] KeyHash { get; set; } = null!;

    #endregion
}
