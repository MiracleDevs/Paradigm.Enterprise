# Paradigm.Enterprise Services

The Paradigm.Enterprise framework includes several service packages that provide abstractions and implementations for common application services such as caching, email, and blob storage. These services follow a consistent pattern and are designed to be easily integrated into applications.

## Services.Core

The Services.Core package defines the foundation for all services in the framework.

### Key Components

- **IService** - Base interface for all services

```csharp
public interface IService
{
}
```

### NuGet Package

```
Install-Package Paradigm.Enterprise.Services.Core
```

## Services.Cache

The Services.Cache package provides caching capabilities for improving application performance.

### Key Components

- **ICacheService** - Interface for cache operations
- **MemoryCacheService** - In-memory cache implementation
- **DistributedCacheService** - Distributed cache implementation
- **FusionCacheService** - Implementation using ZiggyCreatures.FusionCache

### Usage Example

```csharp
// Register cache service
public void ConfigureServices(IServiceCollection services)
{
    services.AddMemoryCache();
    services.AddSingleton<ICacheService, MemoryCacheService>();
}

// Use cache service
public class ProductService
{
    private readonly ICacheService _cacheService;
    private readonly IRepository<Product> _repository;
    
    public ProductService(ICacheService cacheService, IRepository<Product> repository)
    {
        _cacheService = cacheService;
        _repository = repository;
    }
    
    public async Task<Product?> GetByIdAsync(int id)
    {
        var cacheKey = $"product_{id}";
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () => await _repository.GetByIdAsync(id),
            TimeSpan.FromMinutes(10));
    }
}
```

### NuGet Package

```
Install-Package Paradigm.Enterprise.Services.Cache
```

## Services.Email

The Services.Email package provides email sending capabilities.

### Key Components

- **IEmailService** - Interface for email operations
- **SmtpEmailService** - SMTP-based email service implementation
- **EmailMessage** - Model for email messages
- **EmailAttachment** - Model for email attachments

### Usage Example

```csharp
// Register email service
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<SmtpSettings>(Configuration.GetSection("SmtpSettings"));
    services.AddScoped<IEmailService, SmtpEmailService>();
}

// Use email service
public class NotificationService
{
    private readonly IEmailService _emailService;
    
    public NotificationService(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task SendOrderConfirmationAsync(Order order, string customerEmail)
    {
        var emailMessage = new EmailMessage
        {
            To = customerEmail,
            Subject = $"Order Confirmation #{order.Id}",
            Body = $"Thank you for your order. Your order #{order.Id} has been received.",
            IsHtml = true
        };
        
        await _emailService.SendAsync(emailMessage);
    }
}
```

### NuGet Package

```
Install-Package Paradigm.Enterprise.Services.Email
```

## Services.BlobStorage

The Services.BlobStorage package provides abstractions for storing and retrieving files or blobs.

### Key Components

- **IBlobStorageService** - Interface for blob storage operations
- **FileSystemBlobStorageService** - File system implementation
- **AzureBlobStorageService** - Azure Blob Storage implementation
- **BlobMetadata** - Model for blob metadata

### Usage Example

```csharp
// Register blob storage service
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<BlobStorageSettings>(Configuration.GetSection("BlobStorageSettings"));
    services.AddScoped<IBlobStorageService, FileSystemBlobStorageService>();
}

// Use blob storage service
public class DocumentService
{
    private readonly IBlobStorageService _blobStorageService;
    
    public DocumentService(IBlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }
    
    public async Task<string> UploadDocumentAsync(Stream content, string fileName, string contentType)
    {
        var blobId = Guid.NewGuid().ToString();
        
        await _blobStorageService.SaveAsync(content, blobId, new BlobMetadata
        {
            FileName = fileName,
            ContentType = contentType
        });
        
        return blobId;
    }
    
    public async Task<Stream> GetDocumentAsync(string blobId)
    {
        return await _blobStorageService.GetAsync(blobId);
    }
}
```

### NuGet Package

```
Install-Package Paradigm.Enterprise.Services.BlobStorage
```

## Common Features Across Services

1. **Consistent API** - All services follow a consistent pattern
2. **Dependency Injection** - Designed to work with ASP.NET Core dependency injection
3. **Configuration** - Support for configuration via the Options pattern
4. **Extensibility** - Easy to create custom implementations
5. **Testability** - Interfaces make unit testing with mocks straightforward 