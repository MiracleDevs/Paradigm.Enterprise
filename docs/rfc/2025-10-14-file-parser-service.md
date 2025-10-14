# RFC: FileParser Service - Multi-Format File Processing

- **RFC ID**: 2025-10-14-file-parser-service
- **Status**: Draft
- **Author(s)**: Iván Falletti <ivan@miracledevs.com>
- **Created**: 2025-10-14
- **Last Updated**: 2025-10-14

## Summary

This RFC proposes creating a new `Paradigm.Enterprise.Services.FileParser` project that provides a unified service for reading and parsing multiple file formats including Excel (.xlsx, .xls), CSV, JSON, and XML. The service will offer a consistent API for file processing operations while handling format-specific parsing logic internally. This will enable applications to process various file types without needing to implement format-specific parsing logic.

## Motivation

Currently, applications that need to process different file formats must implement separate parsing logic for each format, leading to:

1. **Code duplication**: Similar parsing patterns repeated across different format handlers
2. **Inconsistent error handling**: Each format may handle errors differently
3. **Maintenance overhead**: Multiple parsing implementations to maintain and update
4. **Integration complexity**: Applications need to manage multiple dependencies and APIs
5. **Limited extensibility**: Adding new file formats requires significant changes to consuming applications

By creating a unified FileParser service, we can:

- Provide a consistent API for all supported file formats
- Centralize file parsing logic and error handling
- Enable easy extension to support additional file formats
- Reduce the complexity of file processing in consuming applications
- Standardize file parsing patterns across the Paradigm Enterprise ecosystem

## Detailed Design

### Project Structure

The new project will be located at `src/Paradigm.Enterprise.Services.FileParser/` with the following structure:

```
Paradigm.Enterprise.Services.FileParser/
├── FileParserService.cs
├── IFileParserService.cs
├── Configuration/
│   ├── FileParserConfiguration.cs
│   └── FileFormatConfiguration.cs
├── Parsers/
│   ├── IFileParser.cs
│   ├── ExcelParser.cs
│   ├── CsvParser.cs
│   ├── JsonParser.cs
│   └── XmlParser.cs
├── Models/
│   ├── FileParseResult.cs
│   ├── FileParseOptions.cs
│   └── FileFormat.cs
├── Exceptions/
│   ├── FileParseException.cs
│   ├── UnsupportedFileFormatException.cs
│   └── FileValidationException.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── HealthCheck/
│   └── FileParserHealthCheck.cs
└── Paradigm.Enterprise.Services.FileParser.csproj
```

### Core Interfaces

#### IFileParserService

```csharp
public interface IFileParserService : IService
{
    Task<FileParseResult<T>> ParseFileAsync<T>(
        Stream fileStream, 
        FileFormat format, 
        FileParseOptions options = null) 
        where T : class;
    
    Task<FileParseResult<T>> ParseFileAsync<T>(
        string filePath, 
        FileFormat format, 
        FileParseOptions options = null) 
        where T : class;
    
    Task<bool> ValidateFileAsync(Stream fileStream, FileFormat format);
    Task<bool> ValidateFileAsync(string filePath, FileFormat format);
    Task<FileFormat> DetectFileFormatAsync(Stream fileStream);
    Task<FileFormat> DetectFileFormatAsync(string filePath);
    IEnumerable<FileFormat> GetSupportedFormats();
}
```

#### IFileParser

```csharp
public interface IFileParser
{
    FileFormat SupportedFormat { get; }
    Task<FileParseResult<T>> ParseAsync<T>(Stream fileStream, FileParseOptions options) where T : class;
    Task<bool> ValidateAsync(Stream fileStream);
    bool CanParse(Stream fileStream);
}
```

### Data Models

#### FileParseResult

```csharp
public class FileParseResult<T>
{
    public bool IsSuccess { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public FileParseMetadata Metadata { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

public class FileParseMetadata
{
    public FileFormat Format { get; set; }
    public long FileSizeBytes { get; set; }
    public int RecordCount { get; set; }
    public Dictionary<string, object> FormatSpecificInfo { get; set; } = new();
}
```

#### FileParseOptions

```csharp
public class FileParseOptions
{
    public bool SkipEmptyRows { get; set; } = true;
    public bool ValidateSchema { get; set; } = false;
    public string SchemaPath { get; set; }
    public Dictionary<string, object> FormatSpecificOptions { get; set; } = new();
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
```

#### FileFormat

```csharp
public enum FileFormat
{
    Excel,
    Csv,
    Json,
    Xml
}
```

### Parser Implementations

#### Excel Parser

- Uses `ClosedXML` library for Excel file processing
- Supports both .xlsx and .xls formats
- Handles multiple worksheets
- Provides options for sheet selection and data range specification

#### CSV Parser

- Uses `CsvHelper` library for robust CSV processing
- Supports custom delimiters, headers, and encoding
- Handles quoted fields and escape characters
- Provides validation for CSV structure

#### JSON Parser

- Uses `System.Text.Json` for high-performance JSON processing
- Supports both single objects and arrays
- Provides schema validation capabilities
- Handles large JSON files with streaming support

#### XML Parser

- Uses `System.Xml` for XML processing
- Supports XPath queries for data extraction
- Handles namespaces and complex XML structures
- Provides validation against XSD schemas

### Configuration

#### FileParserConfiguration

```csharp
public class FileParserConfiguration
{
    public int MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public int MaxProcessingTimeSeconds { get; set; } = 300; // 5 minutes
    public bool EnableCaching { get; set; } = false;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public Dictionary<FileFormat, FileFormatConfiguration> FormatConfigurations { get; set; } = new();
}
```

#### FileFormatConfiguration

```csharp
public class FileFormatConfiguration
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Options { get; set; } = new();
}
```

### Service Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileParserService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.Configure<FileParserConfiguration>(configuration.GetSection("FileParser"));
        services.AddSingleton<IFileParserService, FileParserService>();
        services.AddTransient<IExcelParser, ExcelParser>();
        services.AddTransient<ICsvParser, CsvParser>();
        services.AddTransient<IJsonParser, JsonParser>();
        services.AddTransient<IXmlParser, XmlParser>();
        return services;
    }
}
```

### Error Handling

The service implements comprehensive error handling with specific exception types:

- `FileParseException`: General parsing errors
- `UnsupportedFileFormatException`: When format is not supported
- `FileValidationException`: When file validation fails
- `FileSizeExceededException`: When file exceeds size limits
- `ProcessingTimeoutException`: When processing takes too long

### Health Checks

Implements health checks to monitor:
- Parser availability
- Configuration validity
- Dependencies status
- Performance metrics

## Alternatives Considered

### Individual Format Libraries

We considered using individual format-specific libraries directly in consuming applications.

Advantages:
- Direct control over parsing logic
- No additional abstraction layer
- Can use latest features of each library

Disadvantages:
- Code duplication across applications
- Inconsistent error handling
- Multiple dependencies to manage
- No unified configuration approach

### Generic File Processing Library

We considered using existing generic file processing libraries like `FileHelpers` or `CsvHelper` alone.

Advantages:
- Mature libraries with proven track records
- Less custom code to maintain

Disadvantages:
- Limited to specific formats
- No unified API across formats
- May not meet all specific requirements
- Less control over error handling and configuration

### Plugin-Based Architecture

We considered a plugin-based architecture where parsers could be loaded dynamically.

Advantages:
- Highly extensible
- Can add new formats without recompilation
- Flexible deployment options

Disadvantages:
- Increased complexity
- Runtime loading overhead
- More difficult to test and debug
- Potential security concerns

## Testing Strategy

### Unit Tests

1. **Parser Tests**: Individual tests for each format parser
2. **Service Tests**: Tests for the main FileParserService
3. **Configuration Tests**: Tests for configuration binding and validation
4. **Error Handling Tests**: Tests for all exception scenarios
5. **Validation Tests**: Tests for file format detection and validation

### Integration Tests

1. **End-to-End Tests**: Complete file processing workflows
2. **Performance Tests**: Large file processing and memory usage
3. **Format Compatibility Tests**: Various file format versions and edge cases
4. **Concurrent Processing Tests**: Multiple files processed simultaneously

### Test Data

Create comprehensive test data sets including:
- Valid files in all supported formats
- Invalid/corrupted files
- Edge cases (empty files, very large files, malformed data)
- Files with various encoding and formatting options

## Rollout Plan

### Phase 1: Core Infrastructure (Week 1-2)
1. Create project structure and basic interfaces
2. Implement configuration classes and service registration
3. Set up basic error handling and logging

### Phase 2: Parser Implementations (Week 3-4)
1. Implement CSV parser (simplest format)
2. Implement JSON parser
3. Implement XML parser
4. Implement Excel parser

### Phase 3: Service Integration (Week 5)
1. Implement main FileParserService
2. Add format detection logic
3. Implement validation methods
4. Add comprehensive error handling

### Phase 4: Testing and Documentation (Week 6)
1. Complete unit and integration tests
2. Add health checks
3. Create comprehensive documentation
4. Add usage examples

### Phase 5: Release (Week 7)
1. Package and publish NuGet package
2. Update documentation
3. Create migration guide for existing applications

## Dependencies

This RFC depends on:

### NuGet Packages
- **ClosedXML** (for Excel file processing)
- **CsvHelper** (for CSV file processing)
- **System.Text.Json** (for JSON processing)
- **System.Xml** (for XML processing)
- **Microsoft.Extensions.Configuration** (for configuration)
- **Microsoft.Extensions.DependencyInjection** (for DI)
- **Microsoft.Extensions.Logging** (for logging)
- **Microsoft.Extensions.Options** (for options pattern)
- **Microsoft.Extensions.HealthChecks** (for health checks)

### Framework Dependencies
- **.NET 8.0** or higher
- **Paradigm.Enterprise.Services.Core** (for IService interface)

## Configuration Examples

### Basic Configuration

```json
{
  "FileParser": {
    "MaxFileSizeBytes": 104857600,
    "MaxProcessingTimeSeconds": 300,
    "EnableCaching": true,
    "CacheExpiration": "01:00:00",
    "FormatConfigurations": {
      "Excel": {
        "Enabled": true,
        "Options": {
          "DefaultSheetIndex": 0,
          "SkipEmptyRows": true
        }
      },
      "Csv": {
        "Enabled": true,
        "Options": {
          "Delimiter": ",",
          "HasHeader": true,
          "Encoding": "UTF-8"
        }
      }
    }
  }
}
```

### Service Registration

```csharp
// In Program.cs or Startup.cs
builder.Services.AddFileParserService(builder.Configuration);
```

### Usage Examples

```csharp
// Parse Excel file
var result = await fileParserService.ParseFileAsync<Product>(
    "products.xlsx", 
    FileFormat.Excel);

// Parse CSV with custom options
var options = new FileParseOptions
{
    SkipEmptyRows = true,
    FormatSpecificOptions = new Dictionary<string, object>
    {
        ["Delimiter"] = ";",
        ["HasHeader"] = true
    }
};

var csvResult = await fileParserService.ParseFileAsync<Customer>(
    "customers.csv", 
    FileFormat.Csv, 
    options);

// Validate file before parsing
var isValid = await fileParserService.ValidateFileAsync(
    "data.json", 
    FileFormat.Json);

if (isValid)
{
    var jsonResult = await fileParserService.ParseFileAsync<DataModel>(
        "data.json", 
        FileFormat.Json);
}
```

## Performance Considerations

1. **Memory Management**: Implement streaming for large files to avoid memory issues
2. **Caching**: Optional caching for frequently accessed files
3. **Concurrent Processing**: Support for processing multiple files simultaneously
4. **Resource Cleanup**: Proper disposal of file streams and parser resources
5. **Timeout Handling**: Configurable timeouts to prevent long-running operations

## Security Considerations

1. **File Size Limits**: Configurable maximum file sizes to prevent DoS attacks
2. **File Type Validation**: Strict validation of file formats to prevent malicious files
3. **Path Validation**: Secure handling of file paths to prevent directory traversal
4. **Resource Limits**: Memory and CPU usage limits for file processing
5. **Input Sanitization**: Proper sanitization of parsed data

## Open Questions

1. Should we support additional file formats like PDF, Word documents, or proprietary formats?
2. Do we need support for file format conversion between supported types?
3. Should we implement file parsing result caching for performance optimization?
4. Do we need support for streaming large files without loading them entirely into memory?
5. Should we add support for file format detection based on content rather than file extension?
6. Do we need integration with cloud storage services for remote file processing?

## References

- [ClosedXML Documentation](https://closedxml.readthedocs.io/)
- [CsvHelper Documentation](https://joshclose.github.io/CsvHelper/)
- [System.Text.Json Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.text.json)
- [System.Xml Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.xml)
- [Microsoft Extensions Health Checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Paradigm Enterprise Services Architecture](docs/services.md)
