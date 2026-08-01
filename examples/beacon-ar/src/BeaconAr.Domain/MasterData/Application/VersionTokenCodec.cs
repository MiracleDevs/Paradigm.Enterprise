namespace BeaconAr.Domain.MasterData.Application;

public static class VersionTokenCodec
{
    #region Public Methods

    public static string Encode(byte[] value)
    {
        return Operations.VersionTokenCodec.Encode(value);
    }

    public static byte[] Decode(string? value, string fieldName = "version")
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Validation(fieldName, "A version is required.");

        if (!Operations.VersionTokenCodec.TryDecode(value, out byte[] decoded))
            throw Validation(fieldName, "The version must be a canonical SQL Server rowversion token.");
        return decoded;
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
