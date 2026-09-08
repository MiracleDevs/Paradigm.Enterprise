namespace BeaconAr.Data.Sales;

internal static class SalesNumberFormatter
{
    #region Public Methods

    public static string Quote(long value) => Format("Q-", value);

    public static string Order(long value) => Format("SO-", value);

    #endregion

    #region Private Methods

    private static string Format(string prefix, long value)
    {
        if (value is <= 0 or > 99_999_999)
            throw new InvalidOperationException("The sales number sequence exceeded its supported range.");
        return $"{prefix}{value:00000000}";
    }

    #endregion
}
