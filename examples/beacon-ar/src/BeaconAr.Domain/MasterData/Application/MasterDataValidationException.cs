namespace BeaconAr.Domain.MasterData.Application;

public sealed class MasterDataValidationException : MasterDataException
{
    #region Properties

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    #endregion

    #region Constructors

    public MasterDataValidationException(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
        : base("validation_failed", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    #endregion
}
