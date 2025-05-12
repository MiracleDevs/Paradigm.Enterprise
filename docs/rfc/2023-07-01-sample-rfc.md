# RFC: Sample RFC - Standardized Error Handling

- **RFC ID**: 2023-07-01-standardized-error-handling
- **Status**: Implemented
- **Author(s)**: Paradigm Team
- **Created**: 2023-07-01
- **Last Updated**: 2023-07-15

## Summary

This RFC proposes a standardized approach to error handling across the Paradigm Framework. The goal is to create a consistent error handling mechanism that improves developer experience, provides meaningful error messages to users, and simplifies debugging and logging.

## Motivation

Currently, error handling in the Paradigm Framework is inconsistent across different modules. Some components use custom error classes, others throw generic exceptions, and some return error codes or messages. This inconsistency creates several problems:

1. Developers need to learn different error handling patterns for each module
2. Error propagation across module boundaries is complex and error-prone
3. Logging and monitoring are difficult due to inconsistent error formats
4. Client applications receive inconsistent error responses

By standardizing our error handling approach, we can improve code quality, developer experience, and end-user experience.

## Detailed Design

We propose introducing a standardized `ParadigmException` class hierarchy in the `Paradigm.Enterprise.Domain` namespace that all framework components will use:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Domain.Exceptions
{
    /// <summary>
    /// Base exception class for all Paradigm Framework exceptions
    /// </summary>
    public class ParadigmException : Exception
    {
        /// <summary>
        /// Unique error code for this exception
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Optional additional details about the error
        /// </summary>
        public IDictionary<string, object> Details { get; }

        /// <summary>
        /// Suggested HTTP status code for this exception
        /// </summary>
        [JsonIgnore]
        public HttpStatusCode HttpStatusCode { get; }

        public ParadigmException(string message, string code, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError, IDictionary<string, object> details = null)
            : base(message)
        {
            Code = code;
            Details = details ?? new Dictionary<string, object>();
            HttpStatusCode = httpStatusCode;
        }

        public ParadigmException(string message, string code, Exception innerException, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError, IDictionary<string, object> details = null)
            : base(message, innerException)
        {
            Code = code;
            Details = details ?? new Dictionary<string, object>();
            HttpStatusCode = httpStatusCode;
        }

        /// <summary>
        /// Creates a standardized error response object
        /// </summary>
        public virtual ErrorResponse ToErrorResponse()
        {
            return new ErrorResponse
            {
                Message = Message,
                Code = Code,
                Details = Details
            };
        }
    }

    /// <summary>
    /// Standard error response object returned to clients
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Human-readable error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Machine-readable error code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Additional error details
        /// </summary>
        public IDictionary<string, object> Details { get; set; }
    }

    /// <summary>
    /// Exception for validation errors
    /// </summary>
    public class ValidationException : ParadigmException
    {
        public ValidationException(string message, IDictionary<string, object> details = null)
            : base(message, "VALIDATION_ERROR", HttpStatusCode.BadRequest, details)
        {
        }
    }

    /// <summary>
    /// Exception for resource not found errors
    /// </summary>
    public class NotFoundException : ParadigmException
    {
        public NotFoundException(string entityName, string id)
            : base($"{entityName} with id {id} not found", "NOT_FOUND", HttpStatusCode.NotFound)
        {
            Details["entityName"] = entityName;
            Details["id"] = id;
        }
    }

    // Additional exceptions as needed...
}
```

### Error Codes

Error codes will follow a standardized format:

- Category prefix (e.g., AUTH, DATA, CONFIG)
- Underscore separator
- Specific error identifier

For example: `AUTH_INVALID_TOKEN`, `DATA_CONSTRAINT_VIOLATION`

### Integration with Web API

We'll add middleware in the `Paradigm.Enterprise.WebApi` namespace to handle exceptions and return appropriate HTTP responses:

```csharp
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Domain.Exceptions;

namespace Paradigm.Enterprise.WebApi.Middleware
{
    /// <summary>
    /// Middleware to handle exceptions and return standardized error responses
    /// </summary>
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                await HandleExceptionAsync(context, error, _logger);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            ErrorResponse response;

            if (exception is ParadigmException paradigmException)
            {
                // Use HTTP status code and error response from our custom exception
                statusCode = paradigmException.HttpStatusCode;
                response = paradigmException.ToErrorResponse();

                logger.LogError(exception, "A paradigm error occurred: {ErrorCode} - {ErrorMessage}",
                    paradigmException.Code, paradigmException.Message);
            }
            else
            {
                // Handle unexpected exceptions
                response = new ErrorResponse
                {
                    Message = "An unexpected error occurred",
                    Code = "INTERNAL_ERROR",
                    Details = new System.Collections.Generic.Dictionary<string, object>()
                };

                logger.LogError(exception, "An unexpected error occurred");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
```

You would register this middleware in your application's Startup.cs:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Register error handling middleware early in the pipeline
    app.UseMiddleware<ErrorHandlerMiddleware>();

    // Other middleware registrations...
    app.UseRouting();
    app.UseAuthorization();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### Usage in Controllers

Controllers can use these exceptions as follows:

```csharp
using Microsoft.AspNetCore.Mvc;
using Paradigm.Enterprise.Domain.Exceptions;
using Paradigm.Enterprise.Interfaces.Services;
using System.Threading.Tasks;

namespace Paradigm.Enterprise.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                throw new NotFoundException("User", id);
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                var details = new Dictionary<string, object>
                {
                    ["field"] = "email",
                    ["errorType"] = "required"
                };

                throw new ValidationException("Email is required", details);
            }

            // Proceed with user creation...
            var user = await _userService.CreateAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
    }
}
```

## Alternatives Considered

### Result Objects Instead of Exceptions

We considered using a Result pattern (returning `Result<T>` objects with success/error information) instead of exceptions.

Advantages:

- Makes error paths explicit in the method signature
- Can avoid try/catch blocks
- May improve performance for expected failure scenarios

Disadvantages:

- More verbose for the common case
- Requires checking for errors at each function call
- Less familiar to developers used to exception-based error handling in C#
- Doesn't integrate as well with ASP.NET Core middleware

### Framework-agnostic Error Handling

We considered not creating a standardized exception system and instead relying on built-in .NET exceptions.

Advantages:

- Simpler implementation
- No new concepts to learn

Disadvantages:

- Lacks standardized error codes
- More difficult to associate metadata with errors
- Inconsistent error handling across modules
- Limited control over HTTP status codes

## Testing Strategy

We will add:

1. Unit tests for each exception class and middleware
2. Integration tests verifying error propagation through the framework layers
3. API tests confirming proper error responses to client applications

## Rollout Plan

1. Implement the core exception classes in the Domain project
2. Add the error handling middleware to the WebApi project
3. Update existing exception handling in framework components
4. Add documentation and examples
5. Release as part of the next minor version update

## Dependencies

This RFC depends on:

- The existing logging system in the Paradigm Framework
- ASP.NET Core middleware pipeline

## Open Questions

- Should we provide internationalization support for error messages?
- Do we need to support custom exception types defined by applications using our framework?

## References

- [Microsoft Exception Handling Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [ASP.NET Core Error Handling](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [HTTP Status Codes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status)
