namespace BeaconAr.WebApi.Http;

public static class ETagCodec
{
    #region Public Methods

    public static string Format(string version) => $"\"{version}\"";

    public static string ParseRequired(IHeaderDictionary headers) => ParseRequired(headers.IfMatch.ToString());

    public static string ParseRequired(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ApiBoundaryException(428, "precondition_required", "An If-Match header is required.");
        string text = value.Trim();
        if (text.Length < 3 || text[0] != '"' || text[^1] != '"' || text.StartsWith("W/", StringComparison.OrdinalIgnoreCase) || text.Contains(','))
            throw Invalid();
        string version = text[1..^1];
        if (version.Length == 0 || version.Contains('"'))
            throw Invalid();
        return version;
    }

    #endregion

    #region Private Methods

    private static ApiBoundaryException Invalid() =>
        new(400, "invalid_if_match", "If-Match must contain one strong quoted entity tag.");

    #endregion
}
