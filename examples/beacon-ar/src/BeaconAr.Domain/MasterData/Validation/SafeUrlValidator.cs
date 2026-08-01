namespace BeaconAr.Domain.MasterData.Validation;

public static class SafeUrlValidator
{
    #region Public Methods

    public static bool IsSafeThumbnail(string? value)
    {
        if (value is null)
            return true;

        return !ContainsControlCharacter(value) &&
               !value.Contains('{') &&
               !value.Contains('}') &&
               IsSafeAbsoluteUri(value, allowHttp: true);
    }

    public static bool IsSafeTrackingTemplate(string? value)
    {
        if (value is null)
            return true;

        const string token = "{trackingNumber}";
        if (ContainsControlCharacter(value))
            return false;

        string withoutToken = value.Replace(token, "tracking-number", StringComparison.Ordinal);
        if (withoutToken.Contains('{') || withoutToken.Contains('}'))
            return false;

        return IsSafeAbsoluteUri(withoutToken, allowHttp: false);
    }

    #endregion

    #region Private Methods

    private static bool ContainsControlCharacter(string value) => value.Any(char.IsControl);

    private static bool IsSafeAbsoluteUri(string value, bool allowHttp)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || string.IsNullOrWhiteSpace(uri.Host))
            return false;

        bool validScheme = uri.Scheme == Uri.UriSchemeHttps || allowHttp && uri.Scheme == Uri.UriSchemeHttp;
        return validScheme && string.IsNullOrEmpty(uri.UserInfo);
    }

    #endregion
}
