using Paradigm.Web.ApiTemplate.WebApi.Middlewares;

namespace Paradigm.Web.ApiTemplate.WebApi.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Uses the own authentication.
        /// </summary>
        /// <param name="app">The application.</param>
        public static void UseOwnAuthentication(this IApplicationBuilder app)
        {
            app.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}