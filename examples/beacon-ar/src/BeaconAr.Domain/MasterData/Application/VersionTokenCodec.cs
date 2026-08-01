namespace BeaconAr.Domain.MasterData.Application;

public static class VersionTokenCodec
{
    #region Public Methods

    public static string Encode(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToBase64String(value);
    }

    public static byte[] Decode(string? value, string fieldName = "version")
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Validation(fieldName, "A version is required.");

        try
        {
            byte[] decoded = Convert.FromBase64String(value);
            if (decoded.Length != 8 || Convert.ToBase64String(decoded) != value)
                throw Validation(fieldName, "The version must be a canonical SQL Server rowversion token.");

            return decoded;
        }
        catch (FormatException)
        {
            throw Validation(fieldName, "The version must be a canonical SQL Server rowversion token.");
        }
    }

    #endregion

    #region Private Methods

    private static MasterDataValidationException Validation(string fieldName, string message) =>
        new(new Dictionary<string, IReadOnlyList<string>>
        {
            [fieldName] = new[] { message },
        });

    #endregion
}
