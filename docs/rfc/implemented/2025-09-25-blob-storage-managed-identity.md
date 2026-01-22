# RFC: Blob Storage Service - Managed Identity Authentication Support

- **RFC ID**: 2025-09-25-blob-storage-managed-identity
- **Status**: Implemented
- **Author(s)**: Iván Falletti <ivan@miracledevs.com>
- **Created**: 2025-09-25
- **Last Updated**: 2025-09-26

## Summary

This RFC proposes enhancing the `BlobStorageService` in the Paradigm Framework to support Azure Managed Identity authentication while maintaining backward compatibility with connection string-based authentication. This enhancement will improve security by eliminating the need to store connection strings in configuration files and enable better integration with Azure's identity and access management capabilities.

## Motivation

Currently, the `BlobStorageService` only supports connection string-based authentication to Azure Blob Storage. This approach has several limitations:

1. **Security concerns**: Connection strings contain sensitive information (account keys) that must be stored securely
2. **Key management overhead**: Storage account keys need to be rotated periodically and updated across all applications
3. **Limited access control**: Connection strings provide full access to the storage account rather than granular permissions
4. **Compliance requirements**: Many organizations require passwordless authentication for cloud services
5. **Azure best practices**: Microsoft recommends using Managed Identity for Azure service-to-service authentication

By adding Managed Identity support, we can address these concerns while maintaining compatibility with existing implementations.

## Detailed Design

### Core Implementation Requirements

The implementation requires supporting two authentication methods for creating the `BlobServiceClient`:

1. **Connection String (existing)**: `new BlobServiceClient(connectionString)`
2. **Managed Identity (new)**: `new BlobServiceClient(new Uri(storageAccountUri), new DefaultAzureCredential())`

### Required Changes

#### 1. Add New Constructor Overload

Add a new constructor to `BlobStorageService` that accepts a storage account URI and uses `DefaultAzureCredential`:

- Constructor parameter: `Uri storageAccountUri`
- Create client using: `new BlobServiceClient(storageAccountUri, new DefaultAzureCredential())`

#### 2. Update GetBlobStorageContainer Method

The `GetBlobStorageContainer` method needs to handle both authentication modes when creating individual container clients:

- For connection string: Use existing `new BlobContainerClient(connectionString, containerName)`
- For managed identity: Use `new BlobContainerClient(containerUri, new DefaultAzureCredential())`

#### 3. Add Service Registration Extension

Create a new extension method for registering the service with managed identity:

- Method name: `RegisterBlobStorageAccountWithManagedIdentity`
- Configuration: Read storage account name from configuration
- Construct URI: `https://{accountName}.blob.core.windows.net/`

#### 4. Handle GetConnectionString Method

Update the `GetConnectionString()` method to handle cases where managed identity is used:

- Throw `NotSupportedException` when using managed identity
- Maintain existing behavior for connection string authentication

## Configuration Examples

### Managed Identity Configuration

For system-assigned managed identity:

```json
{
  "BlobStorageConfiguration": {
    "AccountName": "mystorageaccount"
  }
}
```

For user-assigned managed identity:

```json
{
  "BlobStorageConfiguration": {
    "AccountName": "mystorageaccount",
    "ClientId": "12345678-1234-1234-1234-123456789012"
  }
}
```

### Service Registration Examples

Using managed identity:

```csharp
builder.Services.RegisterBlobStorageAccountWithManagedIdentity();
```

Using connection string (existing behavior):

```csharp
builder.Services.RegisterBlobStorageAccount("BlobStorageConnectionString");
```

Using automatic detection:

```csharp
builder.Services.RegisterBlobStorageAccountWithAutoAuthentication("BlobStorageConnectionString");
```

## Alternatives Considered

### Token Credential Parameter

We considered adding a `TokenCredential` parameter directly to the constructor, but decided against it because:

- It would require consumers to understand Azure Identity concepts
- Configuration-based approach is more consistent with the framework
- Less flexible for different authentication scenarios

### Environment Variable Configuration

We considered using environment variables for managed identity configuration, but decided against it because:

- Less consistent with the framework's configuration patterns
- Harder to manage multiple storage accounts
- Configuration binding provides better type safety

### Breaking Changes Approach

We considered making breaking changes to simplify the implementation, but decided against it because:

- Backward compatibility is important for existing consumers
- Migration path should be gradual and optional
- Connection string authentication is still valid for some scenarios

## Testing Strategy

We will add:

1. **Unit tests** for each authentication configuration class
2. **Unit tests** for the enhanced BlobStorageService with mocked Azure clients
3. **Integration tests** with both authentication methods using Azure Storage Emulator/Azurite
4. **End-to-end tests** in Azure environment with actual managed identity
5. **Backward compatibility tests** to ensure existing functionality remains intact

## Rollout Plan

1. **Phase 1**: Implement configuration classes and core authentication logic
2. **Phase 2**: Update BlobStorageService with new constructors and authentication support
3. **Phase 3**: Add enhanced service registration extensions
4. **Phase 4**: Update interface and add authentication info method
5. **Phase 5**: Add comprehensive documentation and migration guide
6. **Phase 6**: Release as part of the next minor version update

## Dependencies

This RFC depends on:

- **Azure.Identity** NuGet package (for managed identity support)
- **Azure.Storage.Blobs** NuGet package (existing dependency)
- **Microsoft.Extensions.Configuration** (existing dependency)
- **Microsoft.Extensions.Logging** (existing dependency)
- **Microsoft.Extensions.DependencyInjection** (existing dependency)

## Migration Guide

### For New Applications

Use managed identity registration:

```csharp
builder.Services.RegisterBlobStorageAccountWithManagedIdentity();
```

### For Existing Applications

#### Option 1: Keep Connection String (No Changes Required)

Existing code continues to work without any changes.

#### Option 2: Migrate to Managed Identity

1. Add configuration:
```json
{
  "BlobStorageConfiguration": {
    "AccountName": "mystorageaccount"
  }
}
```

2. Update service registration:
```csharp
// From:
builder.Services.RegisterBlobStorageAccount("BlobStorageConnectionString");

// To:
builder.Services.RegisterBlobStorageAccountWithManagedIdentity();
```

3. Configure Azure RBAC permissions for the managed identity

#### Option 3: Use Automatic Detection

Use the auto-detection method that tries managed identity first, then falls back to connection string:

```csharp
builder.Services.RegisterBlobStorageAccountWithAutoAuthentication("BlobStorageConnectionString");
```

## Security Considerations

1. **Managed Identity Permissions**: Ensure appropriate RBAC roles are assigned (e.g., "Storage Blob Data Contributor")
2. **Principle of Least Privilege**: Use container-level or blob-level permissions when possible
3. **Configuration Security**: Managed identity configuration doesn't contain sensitive data
4. **Backward Compatibility**: Connection strings should still be secured properly when used

## Performance Considerations

1. **Token Caching**: Azure.Identity automatically handles token caching and refresh
2. **Authentication Overhead**: Minimal overhead for token acquisition compared to connection strings
3. **Regional Considerations**: Managed identity works best within the same Azure region

## Open Questions

1. Should we support Azure Key Vault integration for connection strings?
2. Do we need support for other Azure identity types (Service Principal, etc.)?
3. Should we add health checks for managed identity authentication?
4. Do we need metrics/telemetry for authentication method usage?

## References

- [Azure Managed Identity Documentation](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [Azure Blob Storage Authentication](https://docs.microsoft.com/en-us/azure/storage/common/storage-auth)
- [Azure.Identity SDK Documentation](https://docs.microsoft.com/en-us/dotnet/api/azure.identity)
- [Azure Storage RBAC Roles](https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-rbac)