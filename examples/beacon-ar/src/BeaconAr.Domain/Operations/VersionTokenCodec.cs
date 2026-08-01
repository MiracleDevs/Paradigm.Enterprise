namespace BeaconAr.Domain.Operations;

public static class VersionTokenCodec
{
    #region Public Methods

    public static string Encode(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToBase64String(value);
    }

    public static bool TryDecode(string? value, out byte[] decoded)
    {
        decoded = [];
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            decoded = Convert.FromBase64String(value);
            return decoded.Length == 8 && Convert.ToBase64String(decoded) == value;
        }
        catch (FormatException)
        {
            decoded = [];
            return false;
        }
    }

    #endregion
}
