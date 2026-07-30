using Microsoft.AspNetCore.Builder;
using Paradigm.Enterprise.WebApi.Middlewares;

namespace Paradigm.Enterprise.WebApi.Extensions
{
    /// <summary>
    /// Adds Paradigm middleware to an ASP.NET Core request pipeline.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds middleware that converts known provider exceptions to JSON HTTP error responses.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <remarks>Register <see cref="Paradigm.Enterprise.WebApi.Exceptions.Handlers.IExceptionHandler"/>
        /// before using the middleware.</remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddSingleton&lt;IExceptionHandler&gt;(
        ///     new ExceptionHandler(typeof(ApiErrorResources)));
        ///
        /// var app = builder.Build();
        /// app.UseOwnExceptionHandler();
        /// app.MapControllers();
        /// </code>
        /// Place the middleware before components whose exceptions it should translate.
        /// </example>
        public static void UseOwnExceptionHandler(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}
