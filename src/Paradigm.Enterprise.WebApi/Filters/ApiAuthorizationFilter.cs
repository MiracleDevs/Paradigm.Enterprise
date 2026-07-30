using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Paradigm.Enterprise.Providers.Exceptions;

namespace Paradigm.Enterprise.WebApi.Filters
{
    /// <summary>
    /// Authorizes requests by matching an <c>x-api-auth</c> client/secret pair against configuration.
    /// </summary>
    /// <remarks>
    /// The expected header format is <c>client secret</c>. Validation is bypassed while a debugger
    /// is attached; applications must not rely on this filter as their only development-time barrier.
    /// </remarks>
    public class ApiAuthorizationFilter : IAuthorizationFilter
    {
        /// <summary>
        /// The x API authentication
        /// </summary>
        private const string XApiAuth = "x-api-auth";

        /// <summary>
        /// The configuration
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiAuthorizationFilter" /> class.
        /// </summary>
        /// <param name="serviceProvider">The request service provider used to resolve configuration.</param>
        public ApiAuthorizationFilter(IServiceProvider serviceProvider)
        {
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
        }

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
        /// <exception cref="NotAuthorizedException">The header is absent or malformed, the configured
        /// client/secret pair does not match, or no client secrets are configured.</exception>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (System.Diagnostics.Debugger.IsAttached) return;

            const string ExceptionMessage = "The request authorization headers were not provided or are invalid.";

            try
            {
                var clientSecretsConfiguration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _configuration.Bind("ClientSecrets", clientSecretsConfiguration);

                if (clientSecretsConfiguration.Count == 0)
                    throw new NotAuthorizedException("The API configuration is not completed. Please contact an administrator.");

                if (!context.HttpContext.Request.Headers.ContainsKey(XApiAuth))
                    throw new NotAuthorizedException(ExceptionMessage);

                var header = context.HttpContext.Request.Headers[XApiAuth].ToString();
                if (string.IsNullOrWhiteSpace(header))
                    throw new NotAuthorizedException(ExceptionMessage);

                var values = header.Split(" ");
                if (values.Length != 2)
                    throw new NotAuthorizedException(ExceptionMessage);

                var client = values[0];
                var secret = values[1];

                if (!clientSecretsConfiguration.ContainsKey(client) || clientSecretsConfiguration[client] != secret)
                    throw new NotAuthorizedException(ExceptionMessage);
            }
            catch
            {
                throw new NotAuthorizedException(ExceptionMessage);
            }
        }
    }
}
