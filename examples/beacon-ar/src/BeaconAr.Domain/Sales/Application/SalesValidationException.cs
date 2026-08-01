namespace BeaconAr.Domain.Sales.Application;

public sealed class SalesValidationException : SalesException
{
    #region Properties

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    #endregion

    #region Constructors

    public SalesValidationException(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
        : base("validation_failed", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    #endregion
}
