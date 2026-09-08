using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Http;

public static class AuthenticationProblemWriter
{
    #region Public Methods

    public static Task WriteAsync(HttpContext context, int status, string code, string title, string detail)
    {
        ProblemDetails problem = new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Response.StatusCode = status;
        context.Response.ContentType = ApiContract.ProblemJson;
        return context.Response.WriteAsJsonAsync(problem, BeaconArApiJsonContext.Default.ProblemDetails, ApiContract.ProblemJson);
    }

    #endregion
}
