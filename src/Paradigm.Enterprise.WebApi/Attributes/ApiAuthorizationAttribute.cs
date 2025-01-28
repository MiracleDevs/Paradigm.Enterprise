using Microsoft.AspNetCore.Mvc.Filters;
using Paradigm.Enterprise.WebApi.Filters;

namespace Paradigm.Enterprise.WebApi.Attributes;

public class ApiAuthorizationAttribute : Attribute, IFilterFactory
{
    /// <summary>
    /// Gets a value that indicates if the result of <see cref="M:Microsoft.AspNetCore.Mvc.Filters.IFilterFactory.CreateInstance(System.IServiceProvider)" />
    /// can be reused across requests.
    /// </summary>
    public bool IsReusable => false;

    /// <summary>
    /// Creates an instance of the executable filter.
    /// </summary>
    /// <param name="serviceProvider">The request <see cref="T:System.IServiceProvider" />.</param>
    /// <returns>
    /// An instance of the executable filter.
    /// </returns>
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return new ApiAuthorizationFilter(serviceProvider);
    }
}