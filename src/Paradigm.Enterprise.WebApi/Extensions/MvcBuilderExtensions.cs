using Paradigm.Enterprise.WebApi.Filters;

namespace Paradigm.Enterprise.WebApi.Extensions;

/// <summary>
/// Adds Paradigm-specific filters to an MVC application.
/// </summary>
public static class MvcBuilderExtensions
{
    /// <summary>
    /// Adds endpoint exposure control to the MVC pipeline.
    /// This will ensure that only endpoints marked with [ExposeEndpoint] are accessible.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <returns>The MVC builder for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddControllers().AddEndpointExposureControl();
    /// </code>
    /// Actions without <c>[ExposeEndpoint]</c> then return HTTP 404.
    /// </example>
    public static IMvcBuilder AddEndpointExposureControl(this IMvcBuilder builder)
    {
        builder.Services.AddSingleton<EndpointExposureFilter>();

        return builder.AddMvcOptions(options =>
        {
            options.Filters.Add<EndpointExposureFilter>();
        });
    }
}
