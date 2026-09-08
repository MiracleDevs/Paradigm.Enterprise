namespace BeaconAr.Domain.Sales;

public static class MonetaryRounding
{
    #region Constants

    public const int Scale = 2;

    #endregion

    #region Public Methods

    public static decimal Round(decimal value) => decimal.Round(value, Scale, MidpointRounding.AwayFromZero);

    #endregion
}
