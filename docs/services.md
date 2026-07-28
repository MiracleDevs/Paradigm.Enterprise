# Infrastructure services

The service packages wrap common infrastructure without moving application workflows out of providers. They share the marker contract `IService`, which allows convention registration, but several packages require explicit configuration and should not be discovered as default singletons.

## Cache

`AddCacheAsync` registers Redis and `ICacheService`. It uses a named connection string when available, otherwise it builds Azure managed-identity options from `RedisCacheConfiguration`.

```csharp
await builder.Services.AddCacheAsync(
    builder.Configuration,
    "Redis",
    instanceName: "sample:");
```

`ICacheService` requires `JsonTypeInfo<T>` for get and set operations. Include cached types in a source-generated JSON context rather than enabling reflection for convenience.

`RedisCacheConfiguration.ThrowExceptions` controls startup connection behavior. When it is false and a recognized connection, timeout, or authentication failure occurs, registration installs a null-object distributed cache so the host can start. Decide whether degraded startup is acceptable and expose that decision through health checks and telemetry.

## Blob storage

Blob storage supports connection-string and managed-identity registration. Both registrations are scoped.

```csharp
builder.Services.RegisterBlobStorageAccountUsingManagedIdentity(
    "Storage:AccountUri");
```

Use `RegisterBlobStorageAccountUsingConnectionString` only when a connection string is the selected deployment contract. Container and blob operations remain asynchronous and should receive cancellation from the calling workflow where the API allows it.

## Email

`EmailService` uses Azure Communication Services. It binds `EmailConfiguration`, choosing a connection string when present and managed identity otherwise. If required configuration is missing, or if sending fails, it logs and returns rather than propagating the failure.

That behavior makes email best suited to notifications that should not fail the main transaction. A workflow that requires delivery guarantees needs an explicit queue, outbox, or retry design outside this wrapper.

## Table files

The table service reads CSV, Excel, JSON, and XML through one row-oriented API and writes CSV, Excel, or XML from typed data. See [Table files](table-reader.md) for configuration and streaming guidance.

## Registration choices

`RegisterServices` discovers public concrete `IService` implementations as singletons. Put configured or scoped services in its ignore list and register them with their package extension or an application factory. Do not allow discovery to replace an intentional lifetime decision.
