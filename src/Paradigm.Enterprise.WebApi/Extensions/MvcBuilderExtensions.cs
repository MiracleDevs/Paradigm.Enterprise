using Paradigm.Enterprise.WebApi.Filters;

namespace Paradigm.Enterprise.WebApi.Extensions;

public static class MvcBuilderExtensions
{
    /// <summary>
    /// Adds endpoint exposure control to the MVC pipeline.
    /// This will ensure that only endpoints marked with [ExposeEndpoint] are accessible.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <param name="requireExplicitExposure">When true, all endpoints must be explicitly marked with [ExposeEndpoint] to be accessible.</param>
    /// <returns>The MVC builder for chaining.</returns>
    public static IMvcBuilder AddEndpointExposureControl(this IMvcBuilder builder)
    {
        builder.Services.AddSingleton<EndpointExposureFilter>();

        return builder.AddMvcOptions(options =>
        {
            options.Filters.Add<EndpointExposureFilter>();
        });
    }
}