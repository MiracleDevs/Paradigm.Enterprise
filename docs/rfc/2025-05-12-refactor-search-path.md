# RFC: Refactoring Search Path in Controllers, Providers and Repositories

- **RFC ID**: 2025-05-12-refactor-search-path
- **Status**: Implemented
- **Author(s)**: Pablo Ordóñez <pablo@miracledevs.com>
- **Created**: 2025-05-12
- **Last Updated**: 2025-05-12

## Summary

This RFC proposes a restructuring of how search functionality is exposed and implemented across controllers, providers, and repositories in the Paradigm Enterprise Framework. The current design forces the exposure of search methods with fixed parameter types, creating a situation where developers must implement workarounds that lead to redundant code and potentially confusing APIs. This proposal aims to make search operations more flexible, ensuring they are only exposed when needed, and cleanly separating read and edit operations with a clearer class hierarchy.

## Motivation

The current framework architecture has several issues:

1. **Involuntary Forced Search Method Exposure**: Controllers automatically expose search endpoints even when not needed. This creates unnecessary API surface and potential security concerns. It's difficult to see for newcomers that a `ApiControllerBase` without search exists, as it uses the same class name, only with an extra generic parameter.

2. **Fixed Parameter Types**: The search method in `ReadProviderBase` only accepts `FilterTextPaginatedParameters`, forcing developers to create separate methods with custom parameters and override the controller to call them, while leaving the original method unused but exposed.

3. **Inheritance Structure Issues**: The current relationship between `ApiControllerBase` and `ApiControllerCrudBase` doesn't clearly separate read and edit operations, making it difficult to create controllers that only support specific operation types. It also fails to follow the naming convention used elsewhere in the framework.

4. **Method Duplication**: Developers frequently need to create additional search methods with different parameter types while the original method remains exposed but unused.

By addressing these issues, we can create a more intuitive and flexible API for search operations that reduces redundant code and follows cleaner design patterns.

## Detailed Design

### 1. Controller Hierarchy Restructuring

Replace the current controller hierarchy with a more focused approach:

```
ApiControllerBase
├── ReadControllerBase<TProvider, TView, TParameters>
└── EditControllerBase<TProvider, TView, TParameters> : ReadControllerBase<TProvider, TView, TParameters>
```

Each controller type would only expose the methods relevant to its purpose:

```csharp
[AllowAnonymous]
[ApiController]
public abstract class ReadControllerBase<TProvider, TView, TParameters> : ApiControllerBase
    where TProvider : IReadProvider<TView>
    where TParameters : SearchParametersBase
{
    // Constructor
    protected EditControllerBase(ILogger<ApiControllerBase<TProvider, TView, TParameters>> logger, TProvider provider) : base(logger, provider)
    {
    }

    // Search method is now optional through an attribute
    [HttpPost("search")]
    [ExposeEndpoint] // New attribute to control endpoint exposure
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync([FromBody, Required] TParameters parameters)
    {
        return await Provider.SearchAsync(parameters);
    }

    // GetById method
    [HttpGet("get-by-id")]
    [ExposeEndpoint]
    public virtual async Task<TView> GetByIdAsync([FromQuery] int id)
    {
        return await Provider.GetByIdAsync(id);
    }
}

[AllowAnonymous]
public abstract class EditControllerBase<TProvider, TView, TParameters> : ReadControllerBase<TProvider, TView, TParameters>
    where TView : EntityBase, new()
    where TProvider : IEditProvider<TView>
    where TParameters : SearchParametersBase
{
    // Constructor
    protected EditControllerBase(ILogger<ApiControllerBase<TProvider, TView, TParameters>> logger, TProvider provider) : base(logger, provider)
    {
    }

    // Save method
    [HttpPost]
    [ExposeEndpoint]
    public virtual async Task<TView> SaveAsync([FromBody] TView dto)
    {
        return await Provider.SaveAsync(dto);
    }

    // Delete method
    [HttpDelete]
    [ExposeEndpoint]
    public virtual async Task<bool> DeleteAsync([FromQuery] int id)
    {
        return await Provider.DeleteAsync(id);
    }
}
```

### 2. Provider Hierarchy Modification

The provider hierarchy would be updated to match:

```csharp
public interface IReadProvider<TView> : IProvider
{
    // Existing methods

    // Updated search method with generic parameter type
    Task<PaginatedResultDto<TView>> SearchAsync<TParameters>(TParameters parameters)
        where TParameters : SearchParametersBase;
}

public class ReadProviderBase<TInterface, TView, TViewRepository> : ProviderBase, IReadProvider<TView>
{
    // Existing methods

    // Generic search method implementation
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync<TParameters>(TParameters parameters)
        where TParameters : SearchParametersBase
    {
        return await ViewRepository.SearchAsync(parameters);
    }

    // Backward compatibility method
    [Obsolete("Use SearchAsync<TParameters> instead")]
    public virtual async Task<PaginatedResultDto<TView>> SearchPaginatedAsync(FilterTextPaginatedParameters parameters)
    {
        return await SearchAsync(parameters);
    }
}
```

### 3. Base Search Parameters Class

Create a more generic base class for search parameters:

```csharp
public abstract class SearchParametersBase
{
    // Common pagination properties
    public int? PageNumber { get; set; } = 1;
    public int? PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = string.Empty;
    public string? SortDirection { get; set; } = string.Empty;
}

// FilterTextPaginatedParameters would inherit from this
public class FilterTextPaginatedParameters : SearchParametersBase
{
    public string FilterText { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}
```

## Alternatives Considered

### 1. Dynamic Search Method

A possible approach would be to use dynamic parameters:

```csharp
Task<PaginatedResultDto<TView>> SearchAsync(dynamic parameters);
```

This was rejected because it loses type safety and would make the API harder to use and understand. Also dynamic types are slow, and as a policy we don't what to use dynamics.

### 2. Maintaining Current Structure with Optional Methods

We could keep the current structure but make the search method optional:

```csharp
[HttpPost("search")]
[ApiExplorerSettings(IgnoreApi = true)]  // Optionally hide the endpoint
public virtual async Task<PaginatedResultDto<TView>> SearchAsync([FromBody, Required] TParameters parameters)
{
    throw new NotImplementedException("Search is not supported by this controller");
}
```

This was rejected because it leads to poor API design with methods that throw exceptions rather than not existing.

## Testing Strategy

The testing strategy will focus on:

1. **Unit Tests**:

   - Test the new controller hierarchy
   - Verify search functionality works with different parameter types
   - Ensure backward compatibility with existing code

2. **Integration Tests**:

   - Verify endpoint exposure/hiding works correctly
   - Test the full request path from controller to repository

3. **Migration Tests**:
   - Create tests to verify existing code can be migrated to the new structure

## Rollout Plan

1. **Phase 1: Framework Update**

   - Implement the new controller and provider hierarchies
   - Add backward compatibility layers
   - Update documentation

2. **Phase 2: Developer Preview**

   - Release as beta to selected teams
   - Gather feedback and make adjustments

3. **Phase 4: Full Release**

   - Release final version
   - Provide training sessions

4. **Phase 5: Deprecation** (6 months)
   - Mark old methods as deprecated
   - Provide warnings in development mode
   - Plan for eventual removal in a future major version

## Dependencies

- No external dependencies
- Requires coordination with teams using the framework

## Open Questions

1. Should we support custom route naming for endpoints?
2. How should we handle versioning for APIs during migration?
3. How to handle complex search operations that might require non-standard result types?
4. Should we provide default implementations for common search patterns?

## References

- Current implementation in `ApiControllerBase` and `ApiControllerCrudBase`
- Repository pattern best practices
- ASP.NET Core controller conventions
