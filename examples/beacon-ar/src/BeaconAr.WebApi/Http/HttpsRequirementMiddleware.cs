namespace BeaconAr.WebApi.Http;

public sealed class HttpsRequirementMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;

    #endregion

    #region Constructors

    public HttpsRequirementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    #endregion

    #region Public Methods

    public Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.IsHttps)
            return AuthenticationProblemWriter.WriteAsync(context, 400, "https_required", "HTTPS required", "HTTPS is required for this endpoint.");
        return _next(context);
    }

    #endregion
}
