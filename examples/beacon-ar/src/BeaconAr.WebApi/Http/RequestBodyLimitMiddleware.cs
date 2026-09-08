namespace BeaconAr.WebApi.Http;

public sealed class RequestBodyLimitMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;

    #endregion

    #region Constructors

    public RequestBodyLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    #endregion

    #region Public Methods

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1") &&
            context.Request.ContentLength > ApiContract.MaxJsonRequestBodySize)
        {
            return AuthenticationProblemWriter.WriteAsync(
                context,
                StatusCodes.Status413PayloadTooLarge,
                "request_too_large",
                "Request too large",
                "The request body exceeds the configured limit.");
        }

        return _next(context);
    }

    #endregion
}
