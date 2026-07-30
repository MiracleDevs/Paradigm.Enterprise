using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Providers.Exceptions;
using Paradigm.Enterprise.WebApi.Exceptions.Handlers;
using Paradigm.Enterprise.WebApi.JsonSerializerContexts;
using System.Net;
using System.Security.Authentication;
using System.Text;

namespace Paradigm.Enterprise.WebApi.Middlewares;

/// <summary>
/// Converts unhandled request exceptions into JSON error responses and logs the failure.
/// </summary>
/// <remarks>
/// Known provider exceptions map to 401, 403, or 404; all other exceptions map to 500.
/// Set <c>PARADIGM_EXPAND_EXCEPTIONS=true</c> to include inner-exception messages in responses.
/// Do not enable expanded messages where they could disclose sensitive implementation details.
/// </remarks>
public class ExceptionHandlerMiddleware : MiddlewareBase
{
    #region Constructor

    /// <summary>
    /// Initializes the exception-handling middleware.
    /// </summary>
    /// <param name="next">The next request delegate.</param>
    public ExceptionHandlerMiddleware(RequestDelegate next) : base(next)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Invokes the specified context.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public override async Task Invoke(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles the exception asynchronous.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="exception">The exception.</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var handler = context.RequestServices.GetRequiredService<IExceptionHandler>();
        exception = handler.Handle(exception);

        var code = exception switch
        {
            NotFoundException _ => HttpStatusCode.NotFound,
            AuthenticationException _ => HttpStatusCode.Unauthorized,
            NotAuthenticatedException _ => HttpStatusCode.Unauthorized,
            NotAuthorizedException _ => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError
        };

        var result = System.Text.Json.JsonSerializer.Serialize(new Error(GetErrorMessage(exception)), ApiModelsJsonContext.Default.Error);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        await context.Response.WriteAsync(result);

        context.RequestServices
            .GetRequiredService<ILogger<ExceptionHandlerMiddleware>>()
            .LogError(exception, string.Empty);
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="tabs">The tabs.</param>
    /// <returns>The flattened message text.</returns>
    private string GetErrorMessage(Exception exception, string tabs = "")
    {
        var builder = new StringBuilder();
        builder.Append(tabs);
        builder.AppendLine(exception.Message);

        if (Environment.GetEnvironmentVariable("PARADIGM_EXPAND_EXCEPTIONS") == "true")
            if (exception is AggregateException aggregateException)
                foreach (var innerException in aggregateException.InnerExceptions)
                    builder.AppendLine(GetErrorMessage(innerException, tabs + "    "));
            else if (exception.InnerException is not null)
                builder.AppendLine(GetErrorMessage(exception.InnerException, tabs + "    "));

        return builder.ToString().TrimEnd();
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Represents the JSON error body returned to a client.
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        /// Initializes an error body with its client-facing message.
        /// </summary>
        /// <param name="message">The message serialized into the response.</param>
        public Error(string message)
        {
            Message = message;
        }
    }

    #endregion
}
