# 1. Paradigm.Enterprise Services

The Paradigm.Enterprise framework includes several service packages that provide abstractions and implementations for common application services such as caching, email, and blob storage. These services follow a consistent pattern and are designed to be easily integrated into applications.

## 1.1. Services.Core

The Services.Core package defines the foundation for all services in the framework.

### 1.1.1. Key Components

- **IService** - Base interface for all services

```csharp
public interface IService
{
}
```

### 1.1.2. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Services.Core
```

## 1.2. Services.Cache

The Services.Cache package provides caching capabilities for improving application performance.

### 1.2.1. Key Components

- **ICacheService** - Interface for cache operations
- **MemoryCacheService** - In-memory cache implementation
- **DistributedCacheService** - Distributed cache implementation
- **~~FusionCacheService~~** - Implementation using ZiggyCreatures.FusionCache (Removed in version 1.0.5)

### 1.2.2. Usage Example

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

### 1.2.3. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Services.Cache
```

## 1.3. Services.Email

The Services.Email package provides email sending capabilities.

### 1.3.1. Key Components

- **IEmailService** - Interface for email operations
- **SmtpEmailService** - SMTP-based email service implementation
- **EmailMessage** - Model for email messages
- **EmailAttachment** - Model for email attachments

### 1.3.2. Usage Example

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

### 1.3.3. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Services.Email
```

## 1.4. Services.BlobStorage

The Services.BlobStorage package provides abstractions for storing and retrieving files or blobs.

### 1.4.1. Key Components

- **IBlobStorageService** - Interface for blob storage operations
- **FileSystemBlobStorageService** - File system implementation
- **AzureBlobStorageService** - Azure Blob Storage implementation
- **BlobMetadata** - Model for blob metadata

### 1.4.2. Usage Example

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

### 1.4.3. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Services.BlobStorage
```

## 1.5. Services.TableReader

The Services.TableReader package provides capabilities for reading and parsing structured data from various file formats including CSV, JSON, XLS, and XML files.

### 1.5.1. Key Components

- **ITableReaderService** - Interface for table reader operations
- **TableReaderService** - Main service implementation
- **ITableReader** - Interface for reading table data
- **TableReaderConfiguration** - Configuration for table reader settings
- **CsvParserConfiguration** - Specific configuration for CSV parsing
- **TableReaderTypes** - Enumeration of supported file types (CSV, JSON, XLS, XML)

### 1.5.2. Supported File Types

- **CSV** - Comma-separated values with configurable delimiters
- **JSON** - JavaScript Object Notation format
- **XLS** - Microsoft Excel spreadsheet format
- **XML** - Extensible Markup Language format

### 1.5.3. Usage Example

```csharp
// Register table reader service
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<ITableReaderService, TableReaderService>();
}

// Use table reader service
public class DataImportService
{
    private readonly ITableReaderService _tableReaderService;

    public DataImportService(ITableReaderService tableReaderService)
    {
        _tableReaderService = tableReaderService;
    }

    public async Task<List<Product>> ImportProductsFromCsvAsync(Stream csvStream)
    {
        var configuration = new TableReaderConfiguration
        {
            TableReaderType = TableReaderTypes.Csv,
            CsvParserConfiguration = CsvParserConfiguration.Default
        };

        using var reader = _tableReaderService.GetReaderInstance(csvStream, true, configuration);
        var products = new List<Product>();

        while (await reader.ReadRowAsync())
        {
            var row = reader.GetCurrentRow();
            var product = new Product
            {
                Name = row.GetValue<string>("Name"),
                Price = row.GetValue<decimal>("Price"),
                Category = row.GetValue<string>("Category")
            };
            products.Add(product);
        }

        return products;
    }

    public async Task<List<Order>> ImportOrdersFromJsonAsync(byte[] jsonBytes)
    {
        var configuration = new TableReaderConfiguration
        {
            TableReaderType = TableReaderTypes.Json
        };

        using var reader = _tableReaderService.GetReaderInstance(jsonBytes, true, configuration);
        var orders = new List<Order>();

        while (await reader.ReadRowAsync())
        {
            var row = reader.GetCurrentRow();
            var order = new Order
            {
                Id = row.GetValue<int>("Id"),
                CustomerName = row.GetValue<string>("CustomerName"),
                OrderDate = row.GetValue<DateTime>("OrderDate"),
                TotalAmount = row.GetValue<decimal>("TotalAmount")
            };
            orders.Add(order);
        }

        return orders;
    }
}
```

### 1.5.4. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Services.TableReader
```

## 1.6. Common Features Across Services

1. **Consistent API** - All services follow a consistent pattern
2. **Dependency Injection** - Designed to work with ASP.NET Core dependency injection
3. **Configuration** - Support for configuration via the Options pattern
4. **Extensibility** - Easy to create custom implementations
5. **Testability** - Interfaces make unit testing with mocks straightforward
