namespace BeaconAr.WebApi.Http;

public static class ApiContract
{
    #region Constants

    public const string Json = "application/json";
    public const long MaxJsonRequestBodySize = 1_048_576;
    public const string ProblemJson = "application/problem+json";

    #endregion

    #region Public Methods

    public static void EnsurePositiveId(int id, string name = "id")
    {
        if (id <= 0)
            throw new ApiBoundaryException(400, "invalid_query", $"{name} must be a positive integer.");
    }

    public static void SetETag(HttpResponse response, string version) => response.Headers.ETag = ETagCodec.Format(version);

    public static string StatusCode(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "malformed_request",
        StatusCodes.Status404NotFound => "route_not_found",
        StatusCodes.Status405MethodNotAllowed => "method_not_allowed",
        StatusCodes.Status413PayloadTooLarge => "request_too_large",
        StatusCodes.Status415UnsupportedMediaType => "unsupported_media_type",
        _ => "request_failed",
    };

    #endregion
}
