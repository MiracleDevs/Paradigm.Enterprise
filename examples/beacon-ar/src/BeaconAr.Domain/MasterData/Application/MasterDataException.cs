namespace BeaconAr.Domain.MasterData.Application;

public class MasterDataException : Exception
{
    #region Properties

    public string Code { get; }

    public string SafeMessage { get; }

    #endregion

    #region Constructors

    public MasterDataException(string code, string safeMessage, Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        Code = code;
        SafeMessage = safeMessage;
    }

    #endregion
}
