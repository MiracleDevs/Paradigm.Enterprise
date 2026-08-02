namespace BeaconAr.WebApi.Http;

public sealed class ApiBoundaryException : Exception
{
    #region Properties

    public int Status { get; }

    public string Code { get; }

    public string SafeMessage { get; }

    #endregion

    #region Constructors

    public ApiBoundaryException(int status, string code, string safeMessage)
        : base(safeMessage)
    {
        Status = status;
        Code = code;
        SafeMessage = safeMessage;
    }

    #endregion
}
