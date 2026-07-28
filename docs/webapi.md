# Web API

`Paradigm.Enterprise.WebApi` supplies controller bases, exception middleware, endpoint-exposure filtering, model-binding attributes, and convention-based dependency registration. It reduces repeated HTTP plumbing, but the application host still owns routing policy, security, serialization, and middleware ordering.

## Controller bases

`ApiControllerBase<TProvider>` exposes the provider to a custom controller. `ReadApiControllerBase` adds `POST search` and `GET get-by-id`. `EditApiControllerBase` adds `POST` for save and `DELETE` for delete. The read and edit bases require the identifier type explicitly.

```csharp
[Route("catalog-items")]
public sealed class CatalogItemsController
    : EditApiControllerBase<
        ICatalogItemProvider,
        CatalogItemView,
        CatalogItemSearch,
        Guid>
{
    public CatalogItemsController(
        ILogger<CatalogItemsController> logger,
        ICatalogItemProvider provider)
        : base(logger, provider)
    {
    }
}
```

The library bases inherit `AllowAnonymous` metadata. ASP.NET Core skips authorization for an endpoint when that metadata is present. Adding `Authorize` to a derived controller or action, or configuring a fallback policy, does not override it. Use these bases only for endpoints that are intentionally anonymous. For authenticated or authorized endpoints, derive directly from ASP.NET Core's `ControllerBase`, inject the provider, and apply the host's authorization policy there.

## Endpoint exposure

The base endpoints carry `ExposeEndpoint`, but the attribute has no restricting effect by itself. Exposure control becomes active only when MVC is configured with `AddEndpointExposureControl`.

```csharp
builder.Services
    .AddControllers()
    .AddEndpointExposureControl();
```

Once enabled, actions without `ExposeEndpoint` return a not-found result. This reduces accidental route exposure. It is not authentication or authorization, and it must not be used as a security boundary.

## Exception handling

`UseOwnExceptionHandler` adds the library middleware. The middleware delegates exception translation to an `IExceptionHandler`, whose matchers can recognize provider, domain, Entity Framework, or application-specific exceptions.

Register the handler and its matchers deliberately. A bare handler cannot know how every database or business exception should be presented. Place the middleware early enough to cover the components whose exceptions it should translate, while respecting the host's diagnostics and security requirements.

## Serialization

Applications can disable reflection-based System.Text.Json metadata and register generated `JsonSerializerContext` instances. This is useful for predictable serialization and native-AOT preparation, but every request, response, and nested type must be present in a registered context.

When an endpoint fails because metadata is missing, add the type to the application-local generated context or a hand-written context, register it with MVC JSON options, and rebuild. The standalone generator's JSON mode currently has a path-handling defect, and the library does not discover missing JSON types at startup.

## Multipart requests

`MultipartFormDataAttribute` validates the content type. `DisableFormValueModelBindingAttribute` removes default form value providers so a controller can stream a large request. These attributes do not set upload limits or validate file contents. Configure limits and content inspection in the host.
