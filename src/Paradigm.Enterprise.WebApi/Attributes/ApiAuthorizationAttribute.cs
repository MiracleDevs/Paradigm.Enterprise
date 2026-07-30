using Microsoft.AspNetCore.Mvc.Filters;
using Paradigm.Enterprise.WebApi.Filters;

namespace Paradigm.Enterprise.WebApi.Attributes;

/// <summary>
/// Requires a request to pass the Paradigm API-key authorization filter.
/// </summary>
/// <remarks>
/// Apply this attribute to an action or controller that must validate the
/// <c>x-api-auth</c> header against the configured <c>ClientSecrets</c> entries.
/// A new executable filter is constructed for each request using the request service provider.
/// </remarks>
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
