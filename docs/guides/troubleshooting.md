# Troubleshooting

Most integration failures come from conventions that are invisible at the call site. Start with the symptom, then verify the owning layer.

## A repository or provider cannot be resolved

Confirm that the concrete type is public, non-abstract, satisfies the Enterprise repository or provider marker, and implements the exact matching interface name. The application interface should inherit the typed Enterprise contract when consumers or generic registrations need it. Verify the assembly is reachable from the entry assembly or pass it explicitly to registration.

For repositories, a missing matching interface can fail registration. For providers, the type can be skipped silently.

## Search throws `NotImplementedException`

`ReadRepositoryBase.SearchAsync` calls `GetSearchPaginatedFunction`. Override that protected method for any repository exposed through search, or override `SearchAsync` with an appropriate implementation.

## JSON serialization reports missing metadata

Reflection-based serialization is probably disabled and a type is absent from registered contexts. Add the request or response type and its nested types to the source-generated context, register its resolver with MVC, and rebuild. The standalone generator's JSON mode currently has a path-handling defect, so use the application-local process or update the context source directly until that defect is fixed.

## A generated stored procedure cannot map values

Compare result-set order, nullability, and column types with the current database procedure. Confirm the corresponding data-reader and parameter mapper implementations are registered before execution. The standalone mapper generator currently has a path-handling defect and its parameter output is SQL Server-specific. Maintain SQL Server mappers through the application-local process; for PostgreSQL, implement and register `INpgsqlParameterMapper` through `NpgsqlParameterMapperFactory`.

## A base-controller action is visible unexpectedly

Endpoint exposure filtering is opt-in. Confirm `AddEndpointExposureControl` was added to MVC. Then check that only intended actions carry `ExposeEndpoint`.

Exposure does not secure the action. Inspect authentication and authorization separately.

## A request is anonymous unexpectedly

The library controller bases inherit `AllowAnonymous`, which takes precedence over `Authorize` and fallback policies. Move the action to a controller that derives directly from ASP.NET Core's `ControllerBase`, inject the provider there, and apply the host's authorization policy. Verify anonymous and underprivileged requests with API tests.

## A transaction cannot be created

At least one commiteable must be registered before `CreateTransaction` is called. Resolve the participating repositories first so their contexts register with the Unit of Work. Also confirm no transaction is already active.

## Cache startup behaves differently between environments

Check whether the named Redis connection string is present. When it is absent, registration uses the managed-identity section. Review `RedisCacheConfiguration.ThrowExceptions`; when false, recognized failures can install a null cache instead of stopping the host.
