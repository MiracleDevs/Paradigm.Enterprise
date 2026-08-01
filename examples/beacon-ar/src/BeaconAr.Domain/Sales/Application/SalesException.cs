namespace BeaconAr.Domain.Sales.Application;

public class SalesException : Exception
{
    #region Properties

    public string Code { get; }

    public string SafeMessage { get; }

    #endregion

    #region Constructors

    public SalesException(string code, string safeMessage, Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        Code = code;
        SafeMessage = safeMessage;
    }

    #endregion
}
