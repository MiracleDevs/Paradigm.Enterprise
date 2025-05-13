using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Paradigm.Enterprise.WebApi.Attributes;

namespace Paradigm.Enterprise.WebApi.Filters;

/// <summary>
/// Filter that restricts access to endpoints that are not explicitly marked with the ExposeEndpoint attribute.
/// </summary>
public class EndpointExposureFilter : IActionFilter
{
    private readonly bool _requireExplicitExposure;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointExposureFilter"/> class.
    /// </summary>
    /// <param name="requireExplicitExposure">When true, endpoints must have the ExposeEndpoint attribute to be accessible.</param>
    public EndpointExposureFilter(bool requireExplicitExposure = true)
    {
        _requireExplicitExposure = requireExplicitExposure;
    }

    /// <summary>
    /// Called before the action executes.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!_requireExplicitExposure)
            return;

        // Skip this filter in development/debug mode
        if (System.Diagnostics.Debugger.IsAttached)
            return;

        // Get the controller action descriptor
        if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
        {
            // Check if the action method has the ExposeEndpoint attribute
            var hasExposeAttribute = actionDescriptor.MethodInfo.GetCustomAttributes(typeof(ExposeEndpointAttribute), true).Any();

            if (!hasExposeAttribute)
            {
                // If the attribute is not present, return a 404 Not Found to hide the endpoint
                context.Result = new NotFoundResult();
            }
        }
    }

    /// <summary>
    /// Called after the action executes.
    /// </summary>
    /// <param name="context">The action executed context.</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // This method intentionally left empty.
    }
} 