# RFC: Table Writer Service - Export Data to Multiple File Formats

- **RFC ID**: 2025-12-01-table-writer-service
- **Status**: Implemented
- **Author(s)**: Iván Falletti <ivan@miracledevs.com>
- **Created**: 2025-12-01
- **Last Updated**: 2025-12-01

## Summary

This RFC proposes adding a new `TableWriterService` to the `Paradigm.Enterprise.Services.TableReader` library that complements the existing `TableReaderService` by providing the ability to export `IEnumerable<T>` data to multiple file formats (CSV, XLSX, XML). The service will use delegate functions for data extraction instead of reflection, providing flexibility and performance. Additionally, the `TableReaderTypes` enum will be renamed to `TableFileTypes` to reflect its shared use by both reader and writer services.

## Motivation

### Current State

The `Paradigm.Enterprise.Services.TableReader` library currently provides comprehensive read capabilities for CSV, XLS/XLSX, and XML file formats through the `TableReaderService`. However, there is no corresponding write/export functionality, which limits the library's utility for applications that need to export data in these formats.

### Problems This Solves

1. **Missing Export Functionality**: Applications currently need to implement their own export logic or use different libraries for each file format, leading to inconsistent implementations and maintenance overhead.

2. **Inconsistent Data Export**: Without a unified service, different parts of an application may export data differently, making it difficult to maintain consistent formatting and behavior.

3. **Reflection Performance Concerns**: Many existing export libraries rely on reflection to extract property values, which can be slow and inflexible. A delegate-based approach provides better performance and customization.

4. **Enum Naming**: The `TableReaderTypes` enum name is too specific to reading operations, even though it represents file format types that could be shared by both reader and writer services.

### Expected Outcomes

By implementing `TableWriterService`, we will:

- Provide a unified API for exporting data to multiple file formats
- Enable consistent data export across applications
- Offer flexible data transformation through delegate functions
- Improve performance by avoiding reflection
- Create a symmetric API that complements the existing reader service
- Rename the enum to better reflect its shared purpose

## Detailed Design

### Architecture Overview

The `TableWriterService` will mirror the architecture of `TableReaderService`, providing a service layer that delegates to format-specific writer implementations.

```
TableWriterService
├── WriteToStreamAsync<T>() - Write to Stream
├── WriteToBytesAsync<T>() - Write to byte[]
│
└── Writer Implementations
    ├── CsvTableWriter
    ├── XlsTableWriter (using ClosedXML)
    └── XmlTableWriter
```

### Component Changes

#### 1. Enum Rename

**File**: `src/Paradigm.Enterprise.Services.TableReader/Configuration/TableReaderTypes.cs`

Rename `TableReaderTypes` to `TableFileTypes`:

```csharp
public enum TableFileTypes
{
    Csv = 0,
    Xls = 1,
    Xml = 2
}
```

**Impact**: This is a breaking change that requires updating all references throughout the codebase, including:
- `TableReaderService.cs`
- `TableReaderConfiguration.cs`
- `ITableReaderService.cs`
- Test files
- Documentation

**Migration Strategy**: Update all references in a single commit to maintain consistency.

#### 2. ITableWriterService Interface

**File**: `src/Paradigm.Enterprise.Services.TableReader/ITableWriterService.cs`

```csharp
public interface ITableWriterService : IService
{
    Task WriteToStreamAsync<T>(
        Stream targetStream,
        IEnumerable<T> data,
        TableFileTypes format,
        bool includeHeader,
        Func<T, IEnumerable<string>> getColumnValues,
        IEnumerable<string>? columnNames = null,
        TableReaderConfiguration? configuration = null);

    Task<byte[]> WriteToBytesAsync<T>(
        IEnumerable<T> data,
        TableFileTypes format,
        bool includeHeader,
        Func<T, IEnumerable<string>> getColumnValues,
        IEnumerable<string>? columnNames = null,
        TableReaderConfiguration? configuration = null);
}
```

**Key Design Decisions**:

1. **Delegate-Based Data Extraction**: The `getColumnValues` delegate allows callers to specify how to extract column values from each item, avoiding reflection and providing flexibility for custom transformations.

2. **Optional Column Names**: If `columnNames` is null and `includeHeader` is true, column names will be auto-generated (e.g., "Column1", "Column2", etc.).

3. **Header Control**: The `includeHeader` parameter allows callers to control whether headers are written, similar to the reader's `sourceHasHeader` parameter.

4. **Configuration Reuse**: Reuses `TableReaderConfiguration` for consistency, with CSV-specific settings coming from `CsvParserConfiguration`.

#### 3. TableWriterService Implementation

**File**: `src/Paradigm.Enterprise.Services.TableReader/TableWriterService.cs`

The service implementation will:

- Provide two overloads: one for Stream output, one for byte[] output (mirroring `TableReaderService` pattern)
- Use a switch statement based on `TableFileTypes` enum
- Delegate to appropriate writer class (CsvTableWriter, XlsTableWriter, XmlTableWriter)
- Handle null checks and validation
- For byte[] method: create MemoryStream, write to it, then return byte array

#### 4. Writer Implementations

Each writer will be located in `src/Paradigm.Enterprise.Services.TableReader/Writers/{Format}/` directory:

**CsvTableWriter**:
- Use `CsvParserConfiguration` for delimiter, quote, escape character, row delimiter, and culture settings
- Write header row if `includeHeader` is true
- Escape values according to CSV rules using the configuration

**XlsTableWriter**:
- Use ClosedXML library to generate Excel files
- Support XLSX format only (ClosedXML doesn't support legacy XLS)
- Write header row if `includeHeader` is true
- Format cells appropriately based on data types

**XmlTableWriter**:
- Use `System.Xml` to generate table-like XML structure
- Element names come from `columnNames` parameter
- Generate root element containing row elements

### API Usage Examples

#### CSV Export

```csharp
var service = new TableWriterService();
var data = new[] {
    new Person { Name = "John", Age = 25 },
    new Person { Name = "Jane", Age = 30 }
};

var config = new TableReaderConfiguration
{
    TableReaderType = TableFileTypes.Csv,
    CsvParserConfiguration = CsvParserConfiguration.Default
};

await service.WriteToStreamAsync(
    stream,
    data,
    TableFileTypes.Csv,
    includeHeader: true,
    getColumnValues: p => new[] { p.Name, p.Age.ToString() },
    columnNames: new[] { "Name", "Age" },
    configuration: config
);
```

#### XLSX Export

```csharp
await service.WriteToStreamAsync(
    stream,
    data,
    TableFileTypes.Xls,
    includeHeader: true,
    getColumnValues: p => new[] { p.Name, p.Age.ToString() },
    columnNames: new[] { "Name", "Age" }
);
```

### Key Considerations

1. **No Reflection**: All data extraction is done via delegate functions, providing better performance and flexibility.

2. **Null Handling**: Null values are preserved as null and not converted to empty strings. The distinction between null and empty string is maintained, and each format handles null values appropriately (e.g., null remains null in CSV, not converted to an empty string). It is the caller's responsibility to decide how to represent null values through the `getColumnValues` delegate if conversion is desired.

3. **Culture Support**: CSV writing respects the culture settings from `CsvParserConfiguration` for numeric and date formatting.

4. **Async Operations**: Stream writing operations are async to support efficient I/O.

5. **Library Choices**:
   - **ClosedXML**: Chosen for XLSX support (only supports XLSX, not legacy XLS)
   - **System.Xml**: Built-in .NET library for XML

## Alternatives Considered

### Alternative 1: Use Reflection for Data Extraction

**Approach**: Automatically extract property values using reflection based on the type `T`.

**Advantages**:
- Simpler API (no need for delegate function)
- Automatic property discovery

**Disadvantages**:
- Performance overhead from reflection
- Less flexible (can't transform data easily)
- Requires public properties
- Doesn't work well with anonymous types or DTOs with complex structures

**Why Rejected**: Performance and flexibility concerns. Delegate-based approach provides better control and performance.

### Alternative 2: Support Legacy XLS Format

**Approach**: Use a library that supports both XLS and XLSX formats (e.g., EPPlus with additional libraries).

**Advantages**:
- Supports older Excel formats
- Broader compatibility

**Disadvantages**:
- More complex implementation
- Additional dependencies
- XLS format is largely obsolete

**Why Rejected**: ClosedXML is simpler and XLSX is the modern standard. Legacy XLS support can be added later if needed.

### Alternative 3: Keep TableReaderTypes Name

**Approach**: Don't rename the enum, use it as-is for the writer service.

**Advantages**:
- No breaking changes
- Less work

**Disadvantages**:
- Misleading name (not just for readers)
- Inconsistent with the service's dual purpose

**Why Rejected**: The enum name should accurately reflect its purpose. The breaking change is acceptable as part of a major feature addition.

## Testing Strategy

### Unit Tests

Create comprehensive unit tests for:

1. **Each Writer Type**:
   - Test CSV writer with various configurations
   - Test XLSX writer with various data types
   - Test XML writer with different structures

2. **Header Inclusion/Exclusion**:
   - Verify headers are written when `includeHeader` is true
   - Verify no headers when `includeHeader` is false
   - Test auto-generated column names when `columnNames` is null

3. **Data Types**:
   - Primitives (int, string, bool, DateTime, etc.)
   - Nullable types
   - Complex objects (via delegates)

4. **CSV Configuration**:
   - Different delimiters
   - Different quote characters
   - Different escape characters
   - Different cultures

5. **Delegate Functions**:
   - Simple property extraction
   - Transformed values
   - Calculated columns

6. **Enum Rename**:
   - Verify existing reader functionality still works
   - Verify all references updated correctly

7. **Output Methods**:
   - Test Stream output
   - Test byte[] output
   - Verify both produce identical results

### Integration Tests

- Test with real-world data sets
- Test with large datasets (performance)
- Test concurrent writes

## Rollout Plan

### Backward Compatibility

The enum rename (`TableReaderTypes` → `TableFileTypes`) is a **breaking change**. However, since this is part of a major feature addition, we can:

1. Update all internal references in a single commit
2. Update test files
3. Update documentation
4. Release as a minor version bump (e.g., 1.0.21 → 1.1.0) to indicate new features and breaking changes

### Migration Guide

For existing code using `TableReaderTypes`:

**Before**:
```csharp
var config = new TableReaderConfiguration
{
    TableReaderType = TableReaderTypes.Csv
};
```

**After**:
```csharp
var config = new TableReaderConfiguration
{
    TableReaderType = TableFileTypes.Csv
};
```

### Documentation Updates

- Update `docs/table-reader.md` to include writer service documentation
- Update `docs/services.md` to mention the new service
- Add usage examples
- Update enum references

## Dependencies

### New Dependencies

- **ClosedXML**: NuGet package for XLSX file generation
  - Package: `ClosedXML`
  - Version: Latest stable (check compatibility with .NET 9.0)

### Existing Dependencies

- **System.Xml**: Built-in .NET library (no additional package needed)

### Framework Requirements

- .NET 9.0 (current target framework)
- No changes to minimum supported framework version

## Open Questions

1. **Should we support streaming writes for very large datasets?**
   - Current design loads all data into memory for XML
   - Could implement streaming for CSV/XLSX
   - **Recommendation**: Start with current design, add streaming later if needed

2. **Should column names be required when includeHeader is true?**
   - Current design auto-generates if null
   - Could require explicit column names
   - **Recommendation**: Keep auto-generation for convenience

3. **Should we support custom formatting per column?**
   - Current design uses string values from delegate
   - Could add formatting options
   - **Recommendation**: Defer to future enhancement if needed

4. **Should we support multiple sheets in XLSX?**
   - Current design creates single sheet
   - Could add sheet name parameter
   - **Recommendation**: Start with single sheet, add multi-sheet support later

## References

### External Libraries

- [ClosedXML Documentation](https://github.com/ClosedXML/ClosedXML) - Excel file generation
- [System.Xml Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.xml) - XML generation

### Related Documentation

- [TableReader Service Documentation](docs/table-reader.md)
- [Stream-Based Table Reader RFC](docs/rfc/2025-10-28-stream-based-table-reader.md)
- [RFC Template](docs/rfc/template.md)

