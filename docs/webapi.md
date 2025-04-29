# Paradigm.Enterprise.WebApi

The WebApi project provides a comprehensive set of components for building RESTful APIs using ASP.NET Core. It includes base controllers, middleware, exception handling, and other utilities to accelerate API development while following best practices.

## Key Components

### Base Controllers

The WebApi project includes base controllers to simplify API development:

- **ApiControllerBase** - Base controller with common functionality
- **ApiControllerCrudBase** - Controller with CRUD operations for entities

```csharp
[AllowAnonymous]
[ApiController]
public abstract class ApiControllerBase<TProvider> : ControllerBase
    where TProvider : IProvider
{
    protected ILogger Logger { get; }
    protected TProvider Provider { get; }
    
    public ApiControllerBase(ILogger<ApiControllerBase<TProvider>> logger, TProvider provider)
    {
        Logger = logger;
        Provider = provider;
    }
}

[AllowAnonymous]
[ApiController]
public abstract class ApiControllerBase<TProvider, TView, TParameters> : ControllerBase
    where TProvider : IReadProvider<TView>
    where TParameters : FilterTextPaginatedParameters
{
    protected ILogger Logger { get; }
    protected TProvider Provider { get; }
    
    public ApiControllerBase(ILogger<ApiControllerBase<TProvider, TView, TParameters>> logger, TProvider provider)
    {
        Logger = logger;
        Provider = provider;
    }
    
    [HttpPost("search")]
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync([FromBody, Required] TParameters parameters)
    {
        return await Provider.SearchPaginatedAsync(parameters);
    }
}
```

### Middleware

Custom middleware components for common API concerns:

- **ExceptionHandlingMiddleware** - Captures and formats exceptions
- **RequestLoggingMiddleware** - Logs incoming requests and responses
- **ApiKeyAuthenticationMiddleware** - Validates API keys for authentication

### Filters

Action filters for cross-cutting concerns:

- **ValidationFilter** - Validates model state
- **LoggingActionFilter** - Logs action execution
- **CacheFilter** - Enables response caching

### Exception Handling

Standardized exception handling and error responses:

- **ApiException** - Base exception for API errors
- **ValidationException** - Exception for model validation failures
- **NotFoundException** - Exception for not found resources
- **ForbiddenException** - Exception for authorization failures

### Extensions

Extension methods for ASP.NET Core integration:

- **ApplicationBuilderExtensions** - Extensions for IApplicationBuilder
- **ServiceCollectionExtensions** - Extensions for IServiceCollection

## Usage Example

```csharp
// Sample API controller using the base classes
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ApiControllerCrudBase<IProductProvider, ProductView, ProductEdit, ProductSearchParameters>
{
    public ProductsController(
        ILogger<ProductsController> logger,
        IProductProvider provider)
        : base(logger, provider)
    {
    }
    
    // Additional custom endpoints
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedProducts()
    {
        // Custom implementation
        return Ok(await Provider.GetFeaturedProductsAsync());
    }
}

// Register services in Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddParadigmWebApi(options =>
{
    options.EnableExceptionHandling = true;
    options.EnableRequestLogging = true;
    options.EnableModelValidation = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseParadigmWebApi();

app.MapControllers();

app.Run();
```

## Key Features

1. **Standardized API Structure** - Consistent API design with base controllers
2. **Built-in CRUD Operations** - Ready-to-use CRUD endpoints
3. **Exception Handling** - Consistent error responses
4. **Request Validation** - Automatic model validation
5. **Logging** - Request and response logging
6. **Performance** - Optimized JSON serialization

## NuGet Package

```
Install-Package Paradigm.Enterprise.WebApi
``` 