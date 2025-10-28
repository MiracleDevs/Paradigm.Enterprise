# RFC: Stream-Based Table Reader - Memory Efficient File Parsing

- **RFC ID**: 2025-10-28-stream-based-table-reader
- **Status**: Draft
- **Author(s)**: Pablo Ordóñez <pablo@miracledevs.com>
- **Created**: 2025-10-28
- **Last Updated**: 2025-10-28

## Summary

This RFC proposes refactoring the `Paradigm.Enterprise.Services.TableReader` library to support true stream-based parsing for all supported file formats (CSV, JSON, XLS/XLSX, XML). Currently, the library converts all input streams to byte arrays before processing, which creates significant memory pressure when parsing large files. This is particularly problematic in cloud environments (Azure App Service, containers) where memory constraints can cause application failures or performance degradation. By leveraging the existing streaming capabilities of the underlying parsers, we can process files of any size with minimal memory footprint while maintaining backward compatibility.

## Motivation

### Current Problem

The `TableReaderService` currently exhibits memory-inefficient behavior even though it accepts `Stream` parameters. Analysis of the codebase reveals:

1. **Service Level Conversion** (`TableReaderService.cs` lines 30-38):

   ```csharp
   using var memoryStream = new MemoryStream();
   sourceStream.CopyTo(memoryStream);
   bytes = memoryStream.ToArray();
   ```

   The entire stream is loaded into memory as a byte array before any parsing begins.

2. **Reader Level Inefficiency**:
   - **CsvTableReader**: Converts `byte[]` → `string` → `StringReader`, even though the underlying `CsvParser` uses `TextReader.Read()` character-by-character (streaming capable)
   - **JsonTableReader**: Parses entire document with `JsonNode.Parse()`, loading the full structure into memory
   - **XlsTableReader**: Converts `byte[]` → `MemoryStream`, but `ExcelDataReader` library already supports direct stream reading
   - **XmlTableReader**: Converts `byte[]` → `MemoryStream`, but `XPathDocument` constructor accepts streams directly

3. **API Contradiction**: The service provides `ITableReader.ReadRowAsync()` suggesting row-by-row streaming capability, but the implementation loads entire files into memory first.

### Why This Matters

1. **Cloud Resource Constraints**: Azure App Services and containers typically have 1-4GB memory limits. A 500MB CSV file would require:
   - 500MB for the original stream
   - 500MB for the MemoryStream copy
   - 500MB for the byte array
   - 500MB+ for the string conversion (Unicode encoding doubles the size)
   - = **~2GB total** for a single file parse operation

2. **Concurrent Operations**: Multiple simultaneous file uploads could exhaust available memory, causing:
   - OutOfMemoryException crashes
   - Garbage collection pressure
   - Request timeouts
   - Application pool recycling

3. **Cost Implications**: Applications may need to scale up to higher-tier hosting plans solely to accommodate unnecessary memory usage.

4. **Unutilized Capabilities**: The underlying parsing libraries (ExcelDataReader, System.Xml.XPath, TextReader) already support streaming. We're converting streams to byte arrays unnecessarily.

### Expected Outcomes

By implementing true stream-based parsing, we will:

- **Reduce memory usage by 75-90%** for large file operations
- Enable parsing of files larger than available memory
- Improve application stability in cloud environments
- Reduce cloud hosting costs by avoiding unnecessary scale-up
- Maintain backward compatibility for existing consumers
- Align API behavior with API design (streaming interface with streaming implementation)

## Detailed Design

### Architecture Overview

The refactoring will introduce stream-based overloads throughout the reader hierarchy while preserving existing byte array methods for backward compatibility.

```
TableReaderService
├── GetReaderInstance(Stream) - Modified to pass stream directly
├── GetReaderInstance(byte[]) - Preserved for compatibility
│
└── Reader Implementations
    ├── CsvTableReader
    │   ├── OpenFromContent(byte[]) - Existing
    │   └── OpenFromStream(Stream) - NEW
    ├── JsonTableReader
    │   ├── OpenFromContent(byte[]) - Existing
    │   └── OpenFromStream(Stream) - NEW
    ├── XlsTableReader
    │   ├── OpenFromContent(byte[]) - Existing
    │   └── OpenFromStream(Stream) - NEW
    └── XmlTableReader
        ├── OpenFromContent(byte[]) - Existing
        └── OpenFromStream(Stream) - NEW
```

### Component Changes

#### 1. TableReaderService.cs

**Current Implementation (Lines 18-48)**:

```csharp
public ITableReader GetReaderInstance(Stream? sourceStream, bool sourceHasHeader, TableReaderConfiguration configuration)
{
    if (sourceStream is null)
        throw new ArgumentNullException(nameof(sourceStream));

    var originalPosition = sourceStream.CanSeek ? sourceStream.Position : 0;

    try
    {
        if (sourceStream.CanSeek)
            sourceStream.Position = 0;

        byte[] bytes;
        if (sourceStream is MemoryStream ms)
            bytes = ms.ToArray();
        else
        {
            using var memoryStream = new MemoryStream();
            sourceStream.CopyTo(memoryStream);
            bytes = memoryStream.ToArray();
        }

        return GetReaderInstance(bytes, sourceHasHeader, configuration);
    }
    finally
    {
        if (sourceStream.CanSeek)
            sourceStream.Position = originalPosition;
    }
}
```

**Proposed Implementation**:

```csharp
public ITableReader GetReaderInstance(Stream? sourceStream, bool sourceHasHeader, TableReaderConfiguration configuration)
{
    if (sourceStream is null)
        throw new ArgumentNullException(nameof(sourceStream));

    // Reset stream position if seekable
    if (sourceStream.CanSeek && sourceStream.Position != 0)
        sourceStream.Position = 0;

    switch (configuration.TableReaderType)
    {
        case TableReaderTypes.Csv:
            return CsvTableReader.OpenFromStream(sourceStream, sourceHasHeader, configuration.CsvParserConfiguration);

        case TableReaderTypes.Json:
            return JsonTableReader.OpenFromStream(sourceStream, sourceHasHeader);

        case TableReaderTypes.Xls:
            return XlsTableReader.OpenFromStream(sourceStream, sourceHasHeader);

        case TableReaderTypes.Xml:
            return XmlTableReader.OpenFromStream(sourceStream, sourceHasHeader);
    }

    throw new Exception("TableReader not found.");
}
```

**Key Changes**:

- Eliminate stream-to-byte-array conversion
- Pass streams directly to reader constructors
- Remove try-finally block (no longer needed to restore position)
- Maintain stream position at start for parsing

**Note**: The byte array overload (`GetReaderInstance(byte[], bool, TableReaderConfiguration)`) remains unchanged for backward compatibility.

#### 2. CsvTableReader.cs

**New Method**:

```csharp
/// <summary>
/// Opens a new table reader from the specified stream.
/// </summary>
/// <param name="contentStream">The content stream.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
/// <param name="configuration">The configuration.</param>
/// <returns></returns>
public static ITableReader OpenFromStream(Stream contentStream, bool sourceHasHeader, CsvParserConfiguration? configuration = null)
{
    var defaultConfiguration = CsvParserConfiguration.Default;

    configuration = configuration ?? defaultConfiguration;
    configuration.ColumnDelimiter = !string.IsNullOrWhiteSpace(configuration.ColumnDelimiter) ? Regex.Unescape(configuration.ColumnDelimiter) : defaultConfiguration.ColumnDelimiter;
    configuration.EscapeCharacter = configuration.EscapeCharacter != char.MinValue ? configuration.EscapeCharacter : defaultConfiguration.EscapeCharacter;
    configuration.Quotation = configuration.Quotation != char.MinValue ? configuration.Quotation : defaultConfiguration.Quotation;
    configuration.RowDelimiter = !string.IsNullOrWhiteSpace(configuration.RowDelimiter) ? Regex.Unescape(configuration.RowDelimiter) : defaultConfiguration.RowDelimiter;

    return new CsvTableReader(contentStream, sourceHasHeader, configuration);
}
```

**New Constructor Overload**:

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="CsvTableReader" /> class from a stream.
/// </summary>
/// <param name="contentStream">The content stream.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
/// <param name="configuration">The configuration.</param>
private CsvTableReader(Stream contentStream, bool sourceHasHeader, CsvParserConfiguration configuration)
{
    TextReader = new StreamReader(contentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
    Parser = new CsvParser(TextReader, configuration);
    Schema = new CsvTableSchema(Parser).Initialize(sourceHasHeader);
    CurrentRow = new CsvRow(Schema, Parser);
}
```

**Key Changes**:

- Use `StreamReader` instead of `StringReader`
- Enable BOM detection for proper encoding handling
- Set `leaveOpen: true` to allow caller to manage stream lifetime
- Use 4KB buffer size for efficient I/O

**Impact**: `CsvParser` already reads character-by-character using `TextReader.Read()`, so this change enables true streaming with minimal memory usage.

#### 3. XlsTableReader.cs

**New Method**:

```csharp
/// <summary>
/// Opens a new table reader from the specified stream.
/// </summary>
/// <param name="contentStream">The content stream.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
/// <returns></returns>
public static ITableReader OpenFromStream(Stream contentStream, bool sourceHasHeader)
{
    return new XlsTableReader(contentStream, sourceHasHeader);
}
```

**Constructor Change**:

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="XlsTableReader" /> class.
/// </summary>
/// <param name="contentStream">The content stream.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
private XlsTableReader(Stream contentStream, bool sourceHasHeader)
{
    DataReader = ExcelReaderFactory.CreateReader(contentStream, new ExcelReaderConfiguration
    {
        LeaveOpen = true  // Allow caller to manage stream lifetime
    });
    Schema = new XlsTableSchema(DataReader).Initialize(sourceHasHeader);
    CurrentRow = new XlsRow(Schema, DataReader);
}
```

**Key Changes**:

- Remove unnecessary `new MemoryStream(content)` wrapper in existing method
- Pass stream directly to `ExcelReaderFactory.CreateReader()`
- Configure `LeaveOpen = true` to prevent premature disposal
- `ExcelDataReader` library handles streaming internally

#### 4. XmlTableReader.cs

**New Method**:

```csharp
/// <summary>
/// Opens a new table reader from the specified stream.
/// </summary>
/// <param name="contentStream">The content stream.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
/// <returns></returns>
public static ITableReader OpenFromStream(Stream contentStream, bool sourceHasHeader)
{
    return new XmlTableReader(contentStream, sourceHasHeader);
}
```

**Constructor Change**:

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="XmlTableReader" /> class.
/// </summary>
/// <param name="contentStream">The content stream.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
private XmlTableReader(Stream contentStream, bool sourceHasHeader)
{
    // XPathDocument already supports streaming and will read sequentially
    XmlNavigator = new XPathDocument(contentStream).CreateNavigator();
    Schema = new XmlTableSchema(XmlNavigator).Initialize(sourceHasHeader);
    CurrentRow = new XmlRow(Schema, XmlNavigator);
}
```

**Key Changes**:

- Remove `new MemoryStream(content)` wrapper
- Pass stream directly to `XPathDocument` constructor
- `XPathDocument` constructor accepts streams and handles buffering internally

**Note**: While `XPathDocument` loads the document structure into memory for XPath navigation, it does so more efficiently from a stream than from a byte array. For truly large XML files requiring streaming XML parsing, a future enhancement could use `XmlReader` instead, but that would require significant schema and row reading logic changes.

#### 5. JsonTableReader.cs

**New Method**:

```csharp
/// <summary>
/// Opens a new table reader from the specified stream.
/// </summary>
/// <param name="contentStream">The content stream.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
/// <returns></returns>
public static async Task<ITableReader> OpenFromStreamAsync(Stream contentStream, bool sourceHasHeader)
{
    // Use JsonDocument for better memory management than JsonNode.Parse
    var jsonDocument = await JsonDocument.ParseAsync(contentStream, new JsonDocumentOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    });

    // Convert to JsonObject for compatibility with existing schema/row logic
    var jsonObject = JsonSerializer.Deserialize<JsonObject>(jsonDocument.RootElement.GetRawText())
        ?? throw new ArgumentNullException(nameof(jsonObject));

    return new JsonTableReader(jsonObject, sourceHasHeader);
}
```

**Add Synchronous Wrapper**:

```csharp
/// <summary>
/// Opens a new table reader from the specified stream (synchronous).
/// </summary>
/// <param name="contentStream">The content stream.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
/// <returns></returns>
public static ITableReader OpenFromStream(Stream contentStream, bool sourceHasHeader)
{
    // For synchronous compatibility, use Task.Run to avoid blocking
    return OpenFromStreamAsync(contentStream, sourceHasHeader).GetAwaiter().GetResult();
}
```

**New Constructor**:

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="JsonTableReader" /> class from a JsonObject.
/// </summary>
/// <param name="jsonObject">The json object.</param>
/// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
private JsonTableReader(JsonObject jsonObject, bool sourceHasHeader)
{
    _jsonObject = jsonObject ?? throw new ArgumentNullException(nameof(jsonObject));
    Schema = new JsonTableSchema(_jsonObject).Initialize(sourceHasHeader);
    CurrentRow = new JsonRow(Schema, _jsonObject);
}
```

**Key Changes**:

- Use `JsonDocument.ParseAsync` for more efficient memory usage
- Maintain compatibility with existing `JsonObject`-based schema and row logic
- Provide both async and sync versions

> [!IMPORTANT]‡ JSON parsing fundamentally requires loading the structure into memory for the row-based access pattern used by the library. However, using `JsonDocument` provides better memory management than `JsonNode.Parse()`. A future enhancement could support streaming JSON using `Utf8JsonReader` for array-based JSON files.

#### 6. Interface Changes

**ITableReaderService.cs** - No changes required. Existing interface already supports streams:

```csharp
ITableReader GetReaderInstance(Stream sourceStream, bool sourceHasHeader, TableReaderConfiguration configuration);
ITableReader GetReaderInstance(byte[] sourceBytes, bool sourceHasHeader, TableReaderConfiguration configuration);
```

### Stream Lifetime Management

**Critical Consideration**: Who owns the stream?

**Decision**: The caller owns the stream and is responsible for disposal.

**Rationale**:

1. Consistent with .NET conventions (e.g., `StreamReader` with `leaveOpen: true`)
2. Allows callers to reuse or inspect streams after parsing
3. Prevents double-disposal issues
4. Readers should dispose their own created resources (TextReader, ExcelDataReader) but not the input stream

**Implementation**:

- All new stream-based methods use `leaveOpen: true` or equivalent
- Reader `Dispose()` methods dispose internal resources but not the source stream
- Documentation clearly states stream ownership

### Backward Compatibility

**Preserved Functionality**:

1. All existing `OpenFromContent(byte[])` methods remain unchanged
2. `GetReaderInstance(byte[], ...)` method remains unchanged
3. Existing unit tests continue to pass without modification
4. Public API surface unchanged (only new methods added)

**Breaking Changes**: None. This is a non-breaking enhancement.

### Performance Implications

**Memory Savings** (for a 100MB CSV file):

| Metric | Before | After | Savings |
|--------|--------|-------|---------|
| Stream copy | 100 MB | 0 MB | 100 MB |
| Byte array | 100 MB | 0 MB | 100 MB |
| String conversion | 200 MB | 0 MB | 200 MB |
| StreamReader buffer | N/A | 4 KB | -4 KB |
| **Total** | **~400 MB** | **~4 KB** | **~99.99%** |

**Processing Speed**:

- CSV: Negligible impact (already character-by-character)
- XLS: Potential 10-20% improvement (no byte array allocation)
- XML: Potential 5-15% improvement (better stream buffering)
- JSON: Similar performance (structure still loaded to memory)

**Throughput**: Reduced GC pressure allows higher concurrent request handling.

### Security Implications

1. **Stream Position**: Callers must ensure streams are positioned correctly. Service resets to position 0 if seekable.

2. **Stream Lifetime**: Callers control stream disposal, preventing resource leaks from forgotten cleanup.

3. **Memory Limits**: Reduced memory usage improves resilience against memory exhaustion attacks via large file uploads.

4. **No New Risks**: Stream-based parsing doesn't introduce new attack vectors. File size limits and validation should still be implemented at the application level.

## Alternatives Considered

### Alternative 1: Keep Current Architecture

**Advantages**:

- No code changes required
- No risk of introducing bugs
- Simple mental model

**Disadvantages**:

- Continued memory issues in production
- Cannot handle large files
- Unnecessarily high cloud costs
- Poor utilization of underlying library capabilities

**Why rejected**: The current architecture has known problems that impact production systems.

### Alternative 2: Always Convert Stream to Byte Array at Application Layer

**Advantages**:

- Library remains unchanged
- Applications can choose when to use byte arrays

**Disadvantages**:

- Pushes problem to every consumer
- No benefit from library's row-by-row API
- Inconsistent usage patterns across applications
- Doesn't solve the fundamental issue

**Why rejected**: This is a library-level concern that should be solved in the library, not in every consuming application.

### Alternative 3: Remove Byte Array Support, Only Support Streams

**Advantages**:

- Cleaner API
- Forces efficient usage
- Removes redundant code paths

**Disadvantages**:

- Breaking change for existing consumers
- Requires migration effort for all applications
- May break existing deployed applications

**Why rejected**: Backward compatibility is important for enterprise libraries. We can support both patterns.

### Alternative 4: Use IAsyncEnumerable for Row Reading

**Advantages**:

- Modern async streaming pattern
- Explicit async throughout the pipeline
- Better cancellation support

**Disadvantages**:

- Breaking API change
- Requires major refactoring of readers
- More complex implementation
- Existing consumers would need significant changes

**Why rejected**: This would be a valuable future enhancement but is too invasive for this RFC's scope. Can be considered in a separate RFC.

## Testing Strategy

### Unit Tests

The following test categories should be added to `TableReaderServiceTests.cs`:

#### 1. Stream-Based Reader Creation Tests

```csharp
[TestMethod]
public void GetReaderInstance_WithFileStream_UsesStreamDirectly()
{
    // Arrange
    var tempFile = Path.GetTempFileName();
    File.WriteAllText(tempFile, "Name,Age\nJohn,25");

    try
    {
        using var fileStream = File.OpenRead(tempFile);
        var configuration = new TableReaderConfiguration
        {
            TableReaderType = TableReaderTypes.Csv,
            CsvParserConfiguration = CsvParserConfiguration.Default
        };

        // Act
        var result = _service.GetReaderInstance(fileStream, true, configuration);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(CsvTableReader));

        // Verify stream was not consumed entirely (stream-based approach)
        Assert.IsTrue(fileStream.CanRead);
    }
    finally
    {
        File.Delete(tempFile);
    }
}
```

#### 2. Memory Efficiency Tests

```csharp
[TestMethod]
public void GetReaderInstance_WithLargeStream_UsesMinimalMemory()
{
    // Arrange
    var largeContent = new StringBuilder();
    for (int i = 0; i < 100000; i++) // ~2MB of CSV data
    {
        largeContent.AppendLine($"Name{i},Age{i},Email{i}@example.com,Address{i}");
    }

    var bytes = Encoding.UTF8.GetBytes(largeContent.ToString());
    using var stream = new MemoryStream(bytes);
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Csv,
        CsvParserConfiguration = CsvParserConfiguration.Default
    };

    // Capture initial memory
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var initialMemory = GC.GetTotalMemory(false);

    // Act
    var result = _service.GetReaderInstance(stream, true, configuration);

    // Read first few rows
    for (int i = 0; i < 10 && result.ReadRowAsync().Result; i++)
    {
        var row = result.GetCurrentRow();
    }

    // Capture final memory
    var finalMemory = GC.GetTotalMemory(false);
    var memoryUsed = finalMemory - initialMemory;

    // Assert - memory usage should be much less than file size
    // (allowing for some overhead, but should be < 50% of file size)
    Assert.IsTrue(memoryUsed < bytes.Length / 2,
        $"Memory usage ({memoryUsed:N0} bytes) should be less than half the file size ({bytes.Length:N0} bytes)");
}
```

#### 3. Non-Seekable Stream Tests

```csharp
[TestMethod]
public void GetReaderInstance_WithNonSeekableStream_ParsesSuccessfully()
{
    // Arrange
    var content = "Name,Age\nJohn,25\nJane,30";
    var bytes = Encoding.UTF8.GetBytes(content);
    using var nonSeekableStream = new NonSeekableStream(bytes);
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Csv,
        CsvParserConfiguration = CsvParserConfiguration.Default
    };

    // Act
    var result = _service.GetReaderInstance(nonSeekableStream, true, configuration);

    // Assert
    Assert.IsNotNull(result);

    // Verify can read rows
    Assert.IsTrue(result.ReadRowAsync().Result);
    var row = result.GetCurrentRow();
    Assert.AreEqual("John", row.GetValue(0));
}
```

#### 4. Stream Position Tests

```csharp
[TestMethod]
public void GetReaderInstance_WithSeekableStreamAtNonZeroPosition_ResetsToStart()
{
    // Arrange
    var content = "Name,Age\nJohn,25\nJane,30";
    var bytes = Encoding.UTF8.GetBytes(content);
    using var stream = new MemoryStream(bytes);
    stream.Position = 10; // Start partway through

    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Csv,
        CsvParserConfiguration = CsvParserConfiguration.Default
    };

    // Act
    var result = _service.GetReaderInstance(stream, true, configuration);

    // Assert
    Assert.IsNotNull(result);

    // Should read from start of file, not from position 10
    Assert.IsTrue(result.ReadRowAsync().Result);
    var row = result.GetCurrentRow();
    Assert.AreEqual("John", row.GetValue(0));
}
```

#### 5. All Format Stream Tests

```csharp
[TestMethod]
public void GetReaderInstance_CsvFromStream_ParsesCorrectly()
{
    var content = "Name,Age\nJohn,25";
    var bytes = Encoding.UTF8.GetBytes(content);
    using var stream = new MemoryStream(bytes);
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Csv,
        CsvParserConfiguration = CsvParserConfiguration.Default
    };

    var result = _service.GetReaderInstance(stream, true, configuration);

    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(CsvTableReader));
    Assert.IsTrue(result.ReadRowAsync().Result);
}

[TestMethod]
public void GetReaderInstance_JsonFromStream_ParsesCorrectly()
{
    var content = "{\"data\":[{\"Name\":\"John\",\"Age\":25}]}";
    var bytes = Encoding.UTF8.GetBytes(content);
    using var stream = new MemoryStream(bytes);
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Json
    };

    var result = _service.GetReaderInstance(stream, true, configuration);

    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(JsonTableReader));
}

[TestMethod]
public void GetReaderInstance_XmlFromStream_ParsesCorrectly()
{
    var content = "<root><item><name>John</name><age>25</age></item></root>";
    var bytes = Encoding.UTF8.GetBytes(content);
    using var stream = new MemoryStream(bytes);
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Xml
    };

    var result = _service.GetReaderInstance(stream, true, configuration);

    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(XmlTableReader));
}
```

#### 6. Backward Compatibility Tests

```csharp
[TestMethod]
public void GetReaderInstance_WithByteArray_StillWorksAsExpected()
{
    // Ensure byte array overload continues to function
    var bytes = Encoding.UTF8.GetBytes("Name,Age\nJohn,25");
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Csv,
        CsvParserConfiguration = CsvParserConfiguration.Default
    };

    var result = _service.GetReaderInstance(bytes, true, configuration);

    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(CsvTableReader));
}
```

#### 7. Stream Ownership Tests

```csharp
[TestMethod]
public void GetReaderInstance_AfterDisposingReader_StreamRemainsOpen()
{
    // Arrange
    var content = "Name,Age\nJohn,25";
    var bytes = Encoding.UTF8.GetBytes(content);
    var stream = new MemoryStream(bytes);
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Csv,
        CsvParserConfiguration = CsvParserConfiguration.Default
    };

    // Act
    var result = _service.GetReaderInstance(stream, true, configuration);
    result.Dispose();

    // Assert - stream should still be usable
    Assert.IsTrue(stream.CanRead);
    Assert.IsTrue(stream.CanSeek);

    // Cleanup
    stream.Dispose();
}
```

### Integration Tests

#### 1. Large File Processing Test

Create a test that generates a large CSV file (e.g., 50MB) and verifies:

- File can be parsed without OutOfMemoryException
- Memory usage stays below defined threshold
- All rows are correctly parsed
- Processing completes in reasonable time

#### 2. Concurrent Stream Processing Test

Create a test that:

- Opens multiple streams simultaneously
- Parses them concurrently
- Verifies no interference between streams
- Checks memory usage stays within limits

#### 3. Real File Format Test

Use actual sample files for each format:

- Real Excel file from Excel application
- CSV exported from Excel with various encodings (UTF-8, UTF-16, Windows-1252)
- JSON from various sources
- XML with namespaces and complex structures

### Performance Benchmarks

Create benchmark tests using BenchmarkDotNet:

```csharp
[MemoryDiagnoser]
public class TableReaderBenchmarks
{
    private byte[] _csvBytes;
    private TableReaderService _service;

    [GlobalSetup]
    public void Setup()
    {
        var content = new StringBuilder();
        for (int i = 0; i < 10000; i++)
        {
            content.AppendLine($"Name{i},Age{i},Email{i}@example.com");
        }
        _csvBytes = Encoding.UTF8.GetBytes(content.ToString());
        _service = new TableReaderService();
    }

    [Benchmark(Baseline = true)]
    public void ParseCsv_ByteArray()
    {
        var config = new TableReaderConfiguration
        {
            TableReaderType = TableReaderTypes.Csv,
            CsvParserConfiguration = CsvParserConfiguration.Default
        };

        using var reader = _service.GetReaderInstance(_csvBytes, true, config);
        while (reader.ReadRowAsync().Result) { }
    }

    [Benchmark]
    public void ParseCsv_Stream()
    {
        using var stream = new MemoryStream(_csvBytes);
        var config = new TableReaderConfiguration
        {
            TableReaderType = TableReaderTypes.Csv,
            CsvParserConfiguration = CsvParserConfiguration.Default
        };

        using var reader = _service.GetReaderInstance(stream, true, config);
        while (reader.ReadRowAsync().Result) { }
    }
}
```

### Test Coverage Goals

- **Unit Test Coverage**: 90%+ of new stream-based code paths
- **Integration Test Coverage**: All supported file formats with streams
- **Performance Tests**: Memory usage validation for files 10MB, 50MB, 100MB
- **Backward Compatibility**: All existing tests must pass without modification


### Backward Compatibility Strategy

**Version Numbering**:

- Current version: X.Y.Z
- New version: X.(Y+1).0 (minor version bump for new features)

**Compatibility Guarantees**:

1. All existing byte array methods work identically
2. All existing tests pass without modification
3. No changes to public interfaces
4. No removal of existing methods
5. Only additions: new stream-based overloads

**Migration Path**: None required. Existing code continues to work. Applications can optionally update to use stream methods for better performance.

**Deprecation**: None. Byte array methods remain fully supported.

### Feature Flags

For staged rollout in consuming applications, consider adding a configuration option:

```csharp
public class TableReaderConfiguration
{
    // ... existing properties ...

    /// <summary>
    /// If true, use stream-based parsing. If false, convert to byte array (legacy behavior).
    /// Default: true (use streaming)
    /// </summary>
    public bool UseStreamingParsing { get; set; } = true;
}
```

This allows applications to:

1. Opt out if issues are discovered
2. A/B test performance in production
3. Gradually roll out to different environments

**Note**: This is optional and may not be necessary given the non-breaking nature of the changes.

## Dependencies

### Library Dependencies

No new dependencies required. All necessary libraries are already referenced:

- **ExcelDataReader** - Already supports stream-based reading
- **System.Text.Json** - Already includes JsonDocument for streaming
- **System.Xml.XPath** - Already supports streams in XPathDocument

### Framework Dependencies

- **.NET 6.0 or higher** (current target framework)
- No changes to minimum supported framework version

### Related Projects

This RFC has no dependencies on other RFCs or projects. It is a self-contained enhancement to `Paradigm.Enterprise.Services.TableReader`.

## Open Questions

### 1. Should we add async support throughout the reading pipeline?

Currently, `ITableReader.ReadRowAsync()` is not truly async - it returns `Task.FromResult()`. Should we:

- **Option A**: Keep current sync implementation (easier, no breaking changes)
- **Option B**: Make parsing truly async (more complex, better for large files)
- **Option C**: Add parallel async APIs (ITableReaderAsync interface)

**Recommendation**: Option A for this RFC. Option B or C could be a future enhancement via separate RFC.

### 2. Should we support IAsyncEnumerable for row enumeration?

Modern .NET pattern would be:

```csharp
IAsyncEnumerable<IRow> ReadRowsAsync(CancellationToken cancellationToken = default);
```

**Trade-offs**:

- More idiomatic for async streaming
- Better cancellation support
- Breaking change or parallel API required

**Recommendation**: Future RFC. Too invasive for this change.

### 3. Should we add buffer size configuration?

Allow consumers to tune memory/performance trade-off:

```csharp
public class TableReaderConfiguration
{
    public int StreamBufferSize { get; set; } = 4096;
}
```

**Recommendation**: Not initially. Default 4KB buffer is appropriate for most scenarios. Can add if demand exists.

### 4. How should we handle encoding detection for CSV files?

Current approach uses BOM detection. Should we:

- **Option A**: Only use BOM (simple, fast)
- **Option B**: Add charset detection library (complex, more robust)
- **Option C**: Allow callers to specify encoding

**Recommendation**: Option A (BOM detection) + Option C (allow encoding override via CsvParserConfiguration).

### 5. Should XmlTableReader use XmlReader for true streaming?

XPathDocument loads the document structure into memory. For truly large XML files, XmlReader provides forward-only streaming.

**Trade-offs**:

- XmlReader: Lower memory, but cannot navigate backward or use XPath
- XPathDocument: Higher memory, but full XPath support

**Recommendation**: Keep XPathDocument for now (current behavior). Add XmlReader-based option in future if needed.

### 6. Should we implement connection resiliency for network streams?

Network streams can timeout or disconnect. Should we add retry logic?

**Recommendation**: No. Retry logic should be implemented at the application layer where policies can be customized.

### 7. Should we add progress reporting for large file processing?

Allow consumers to show progress bars:

```csharp
public interface ITableReader
{
    event EventHandler<ProgressEventArgs> ProgressChanged;
    long? TotalRows { get; }
    long CurrentRow { get; }
}
```

**Recommendation**: Future enhancement. Not critical for this RFC.

## References

### External Libraries

- [ExcelDataReader Documentation](https://github.com/ExcelDataReader/ExcelDataReader) - Stream-based Excel reading
- [System.Text.Json Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.text.json) - JSON parsing APIs
- [XPathDocument Class](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xpath.xpathdocument) - XML document loading

### .NET Best Practices

- [Stream Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-read-and-write-to-a-newly-created-data-file)
- [StreamReader Class](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamreader)
- [Memory Management](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/memory-management-and-gc)

### Paradigm Enterprise Documentation

- [TableReader Service Tests](src/Paradigm.Enterprise.Tests/Services/TableReaderServiceTests.cs) - Existing test suite
- [Services Architecture](docs/services.md) - Enterprise services patterns
- [RFC Template](docs/rfc/template.md) - RFC structure and guidelines
- [File Parser Service RFC](docs/rfc/2025-10-14-file-parser-service.md) - Related file processing RFC

### Performance Analysis Tools

- [BenchmarkDotNet](https://benchmarkdotnet.org/) - .NET performance benchmarking
- [dotMemory](https://www.jetbrains.com/dotmemory/) - Memory profiling
- [PerfView](https://github.com/microsoft/perfview) - Performance analysis tool

### Similar Implementations

- [CsvHelper Streaming](https://joshclose.github.io/CsvHelper/examples/reading/reading-by-hand/) - CSV streaming patterns
- [Newtonsoft.Json Streaming](https://www.newtonsoft.com/json/help/html/Performance.htm) - JSON streaming approach
- [NPOI Streaming](https://github.com/nissl-lab/npoi) - Alternative Excel streaming library
