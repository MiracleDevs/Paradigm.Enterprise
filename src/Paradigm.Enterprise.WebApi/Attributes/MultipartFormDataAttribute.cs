using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Paradigm.Enterprise.WebApi.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class MultipartFormDataAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        if (request.HasFormContentType && request.ContentType is not null &&
            request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return;

        context.Result = new StatusCodeResult(Microsoft.AspNetCore.Http.StatusCodes.Status415UnsupportedMediaType);
    }
}