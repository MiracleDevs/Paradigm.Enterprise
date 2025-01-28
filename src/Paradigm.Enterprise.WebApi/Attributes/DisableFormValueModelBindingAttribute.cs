using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Paradigm.Enterprise.WebApi.Attributes;

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
        //factories.RemoveType<FormFileValueProviderFactory>();
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