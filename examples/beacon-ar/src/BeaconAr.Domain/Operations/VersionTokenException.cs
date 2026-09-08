namespace BeaconAr.Domain.Operations;

public sealed class VersionTokenException : Exception
{
    #region Constructors

    public VersionTokenException()
        : base("The version must be a canonical SQL Server rowversion token.")
    {
    }

    #endregion
}
