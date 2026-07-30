using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Paradigm.Enterprise.WebApi.Attributes;

/// <summary>
/// Prevents ASP.NET Core from buffering multipart form values before an action reads the request body.
/// </summary>
/// <remarks>
/// Use this attribute for streaming upload endpoints. The action is responsible for parsing and
/// validating the request body because the form, file, and jQuery form value providers are removed.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    /// <summary>
    /// Executes the resource filter. Called before execution of the remainder of the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext" />.</param>
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var factories = context.ValueProviderFactories;
        factories.RemoveType<FormValueProviderFactory>();
        factories.RemoveType<FormFileValueProviderFactory>();
        factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    /// <summary>
    /// Executes the resource filter. Called after execution of the remainder of the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ResourceExecutedContext" />.</param>
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
