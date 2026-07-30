using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Paradigm.Enterprise.WebApi.Attributes;

/// <summary>
/// Rejects requests whose content type is not <c>multipart/form-data</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class MultipartFormDataAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Validates the request content type before the action executes.
    /// </summary>
    /// <param name="context">The action context whose request is inspected.</param>
    /// <remarks>
    /// Invalid requests are short-circuited with HTTP 415 (Unsupported Media Type).
    /// </remarks>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        if (request.HasFormContentType && request.ContentType is not null &&
            request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return;

        context.Result = new StatusCodeResult(Microsoft.AspNetCore.Http.StatusCodes.Status415UnsupportedMediaType);
    }
}
