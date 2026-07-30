using Microsoft.AspNetCore.Mvc.Filters;

namespace Paradigm.Enterprise.WebApi.Attributes;

/// <summary>
/// Marks an API action as eligible for exposure when endpoint-exposure filtering is enabled.
/// </summary>
/// <remarks>
/// This attribute is a marker: its <see cref="IActionFilter"/> callbacks perform no enforcement.
/// The application must register <c>AddEndpointExposureControl()</c> to install the global filter
/// that returns HTTP 404 for unmarked actions. The marker is not an authentication or authorization
/// mechanism.
/// </remarks>
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
