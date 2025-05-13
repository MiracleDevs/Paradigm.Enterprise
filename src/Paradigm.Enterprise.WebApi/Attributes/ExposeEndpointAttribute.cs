using Microsoft.AspNetCore.Mvc.Filters;

namespace Paradigm.Enterprise.WebApi.Attributes;

/// <summary>
/// Attribute used to mark API endpoints that should be exposed to external systems.
/// When not applied to an endpoint, the endpoint will remain internal and not publicly accessible.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ExposeEndpointAttribute : Attribute, IActionFilter
{
    /// <summary>
    /// Called before the action executes.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // This method intentionally left empty. The attribute's presence is checked by middleware or filters,
        // but doesn't affect the action's execution directly.
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