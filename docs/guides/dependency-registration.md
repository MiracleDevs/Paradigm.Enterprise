# Dependency registration

The Web API package can discover repositories, providers, services, mappers, entities, and DTOs. Discovery reduces composition-root repetition, but it relies on exact naming, inheritance, visibility, and assembly reachability.

## Naming is executable behavior

A public concrete `CatalogItemRepository` must implement `ICatalogItemRepository`. A public concrete `CatalogItemProvider` must implement `ICatalogItemProvider`. The repository registration expects the matching interface and can fail when it is absent. Provider registration skips a type when the matching interface cannot be found.

Discovery first filters the concrete class by the relevant Enterprise marker, such as `IRepository` or `IProvider`. It then registers the interface whose name is exactly `I{ConcreteName}`. The exact-name interface is normally designed to inherit the typed Enterprise contract so consumers receive that contract and the helper can register its inherited generic interfaces. Reflection discovery does not independently require that inheritance when the concrete class already satisfies the marker.

## Assembly discovery

With no explicit assemblies, the helpers inspect the entry assembly and assemblies referenced by it. A plugin or indirectly loaded module may not be reachable through that graph. Pass explicit assembly roots when the application loads modules dynamically or when a project is not referenced by the host.

```csharp
builder.Services.RegisterRepositories(
    typeof(CatalogItemRepository).Assembly);

builder.Services.RegisterProviders(
    typeof(CatalogItemProvider).Assembly);
```

Passing an assembly changes the discovery root, so include every module that should participate.

## Lifetimes

Registration uses these lifetimes:

| Category | Lifetime |
| --- | --- |
| Repositories | Transient |
| Providers | Transient |
| Mappers | Transient |
| Entities and DTOs | Transient |
| Convention-discovered `IService` types | Singleton |
| `IUnitOfWork` and `DbContext` | Application registration, normally scoped |

`RegisterServices` accepts an ignore list because configured services often need explicit factories or a different lifetime. Add those types to the ignore list and register them separately.

```csharp
builder.Services
    .RegisterServices(
        [typeof(ConfiguredConnector)])
    .RegisterMappers()
    .RegisterEntities()
    .RegisterDtos();

builder.Services.AddScoped<IConfiguredConnector, ConfiguredConnector>();
```

Do not allow a singleton service to capture a scoped repository, context, or Unit of Work.

## Recommended registration order

Register configuration and database connection providers first, followed by contexts and Unit of Work. Register explicitly configured infrastructure services next. Run convention discovery after those dependencies exist. Add generated mapper registration and JSON contexts before building the host.

Registration order does not repair an invalid lifetime, but it makes the composition root readable and avoids factories that run before their prerequisites are present.
