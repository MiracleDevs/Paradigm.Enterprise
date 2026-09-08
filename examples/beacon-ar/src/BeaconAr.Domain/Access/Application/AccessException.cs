namespace BeaconAr.Domain.Access.Application;

public sealed class AccessException : Exception
{
    #region Properties

    public string Code { get; }

    public string SafeMessage { get; }

    #endregion

    #region Constructors

    public AccessException(string code, string safeMessage, Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        Code = code;
        SafeMessage = safeMessage;
    }

    #endregion
}
