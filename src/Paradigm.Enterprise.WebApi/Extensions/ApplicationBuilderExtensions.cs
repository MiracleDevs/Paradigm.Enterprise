using Microsoft.AspNetCore.Builder;
using Paradigm.Enterprise.WebApi.Middlewares;

namespace Paradigm.Enterprise.WebApi.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Uses the own exception handler.
        /// </summary>
        /// <param name="app">The application.</param>
        public static void UseOwnExceptionHandler(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}