# 1. Paradigm.Enterprise.TableReader

The Table Reader library provides a unified, stream-based API for reading and parsing structured data from various file formats. It enables backend .NET C# developers to process CSV, JSON, XLS, and XML files efficiently with a consistent interface, supporting both streaming and in-memory data sources.

## 1.1. Overview

The `Paradigm.Enterprise.Services.TableReader` package abstracts the complexity of parsing different file formats behind a simple, row-by-row reading interface. Whether you're importing data from CSV files, processing JSON arrays, reading Excel spreadsheets, or parsing XML documents, the Table Reader provides a consistent API that simplifies data import operations.

### 1.1.1. Key Features

- **Unified API** - Single interface for multiple file formats
- **Stream-Based Processing** - Efficient memory usage for large files
- **Type-Safe Access** - Strongly-typed value retrieval methods
- **Schema Discovery** - Automatic column detection and metadata access
- **Flexible Configuration** - Customizable parsing options per file type
- **Async Support** - Asynchronous row reading for better performance

### 1.1.2. Supported File Types

- **CSV** - Comma-separated values with configurable delimiters, quotation, and escape characters
- **JSON** - JavaScript Object Notation format (arrays of objects)
- **XLS/XLSX** - Microsoft Excel spreadsheet format (BIFF2-8 and Office Open XML)
- **XML** - Extensible Markup Language format

## 1.2. Architecture and Key Components

The Table Reader library follows a layered architecture with clear separation between service, reader, schema, and row abstractions.

### 1.2.1. Service Layer

#### ITableReaderService

The main service interface that provides factory methods for creating table reader instances.

```csharp
public interface ITableReaderService : IService
{
    ITableReader GetReaderInstance(Stream sourceStream, bool sourceHasHeader, TableReaderConfiguration configuration);
    ITableReader GetReaderInstance(byte[] sourceBytes, bool sourceHasHeader, TableReaderConfiguration configuration);
}
```

**Key Methods:**

- `GetReaderInstance(Stream, bool, TableReaderConfiguration)` - Creates a reader from a stream
- `GetReaderInstance(byte[], bool, TableReaderConfiguration)` - Creates a reader from a byte array

**Parameters:**

- `sourceStream` / `sourceBytes` - The data source to read from
- `sourceHasHeader` - Indicates whether the first row/record contains column names
- `configuration` - Configuration object specifying the file type and parsing options

#### TableReaderService

The default implementation of `ITableReaderService` that creates appropriate reader instances based on the configuration.

### 1.2.2. Reader Layer

#### ITableReader

The core interface for reading table data row by row. It provides access to the table schema and enables iteration through data rows.

```csharp
public interface ITableReader : IDisposable
{
    ITableSchema Schema { get; }
    Task<bool> ReadRowAsync();
    IRow GetCurrentRow();
}
```

**Key Members:**

- `Schema` - Provides access to column metadata
- `ReadRowAsync()` - Advances to the next row and returns `true` if a row is available
- `GetCurrentRow()` - Returns the current row data

**Usage Pattern:**
The reader follows a forward-only cursor pattern. Call `ReadRowAsync()` to advance to the next row, then use `GetCurrentRow()` to access the row data.

### 1.2.3. Schema Layer

#### ITableSchema

Provides metadata about the table structure, including column names, indices, and types.

```csharp
public interface ITableSchema
{
    ITableSchema Initialize(bool sourceHasHeader);
    IEnumerable<IColumn> GetColumns();
    IColumn? GetColumn(string columnName);
    IColumn? GetColumn(int index);
    IColumn GetRequiredColumn(string columnName);
}
```

**Key Methods:**

- `GetColumns()` - Returns all columns in the table
- `GetColumn(string)` - Retrieves a column by name (case-insensitive), returns `null` if not found
- `GetColumn(int)` - Retrieves a column by index
- `GetRequiredColumn(string)` - Retrieves a column by name, throws `InvalidOperationException` if not found

#### IColumn

Represents a single column in the table schema.

```csharp
public interface IColumn
{
    int Index { get; }
    string Name { get; }
    Type Type { get; }
}
```

**Properties:**

- `Index` - Zero-based column position
- `Name` - Column name (from header row or auto-generated)
- `Type` - .NET type representation of the column data

### 1.2.4. Row Layer

#### IRow

Provides access to cell values in the current row. Supports both index-based and column-based access, with strongly-typed retrieval methods.

```csharp
public interface IRow
{
    int Index { get; }
    object? this[int index] { get; }

    // Type-specific getters by index
    string? GetString(int index);
    int GetInt32(int index);
    decimal GetDecimal(int index);
    DateTime GetDateTime(int index);
    bool GetBoolean(int index);
    // ... and more

    // Type-specific getters by column
    string? GetString(IColumn column);
    int GetInt32(IColumn column);
    decimal GetDecimal(IColumn column);
    // ... and more

    object? GetValue(int index);
    object? GetValue(IColumn column);
    bool IsNull(int index);
    bool IsNull(IColumn column);
}
```

**Key Methods:**

- `GetString(IColumn)` / `GetString(int)` - Retrieves a string value
- `GetInt32(IColumn)` / `GetInt32(int)` - Retrieves a 32-bit integer value
- `GetDecimal(IColumn)` / `GetDecimal(int)` - Retrieves a decimal value
- `GetDateTime(IColumn)` / `GetDateTime(int)` - Retrieves a DateTime value
- `GetBoolean(IColumn)` / `GetBoolean(int)` - Retrieves a boolean value
- `GetValue(IColumn)` / `GetValue(int)` - Retrieves the raw object value
- `IsNull(IColumn)` / `IsNull(int)` - Checks if a value is null

**Supported Types:**
The interface provides getters for all common .NET types: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `char`, `string`, `DateTime`, and `bool`.

### 1.2.5. Configuration

#### TableReaderConfiguration

Configuration class that specifies the file type and format-specific options.

```csharp
public class TableReaderConfiguration
{
    public TableReaderTypes TableReaderType { get; set; }
    public CsvParserConfiguration? CsvParserConfiguration { get; set; }
    public bool IndentResults { get; set; }
}
```

**Properties:**

- `TableReaderType` - The file format type (CSV, JSON, XLS, or XML)
- `CsvParserConfiguration` - CSV-specific parsing options (required for CSV files)
- `IndentResults` - Formatting option for output (typically used for debugging)

#### TableReaderTypes

Enumeration of supported file formats.

```csharp
public enum TableReaderTypes
{
    Csv = 0,
    Json = 1,
    Xls = 2,
    Xml = 3
}
```

#### CsvParserConfiguration

Configuration for CSV file parsing, allowing customization of delimiters, quotation, and culture settings.

```csharp
public class CsvParserConfiguration
{
    public static readonly CsvParserConfiguration Default = new()
    {
        RowDelimiter = "\r\n",
        ColumnDelimiter = ",",
        Quotation = '"',
        EscapeCharacter = '\\',
        Culture = CultureInfo.InvariantCulture
    };

    public string? ColumnDelimiter { get; set; }
    public string? RowDelimiter { get; set; }
    public char? Quotation { get; set; }
    public char? EscapeCharacter { get; set; }
    public IFormatProvider? Culture { get; set; }
}
```

**Properties:**

- `ColumnDelimiter` - Character(s) that separate columns (default: `","`)
- `RowDelimiter` - Character(s) that separate rows (default: `"\r\n"`)
- `Quotation` - Character used to quote field values (default: `'"'`)
- `EscapeCharacter` - Character used to escape special characters (default: `'\'`)
- `Culture` - Culture info for numeric and date parsing (default: `InvariantCulture`)

## 1.3. Getting Started

### 1.3.1. NuGet Package

Install the Table Reader package using NuGet Package Manager or the .NET CLI:

```shell
Install-Package Paradigm.Enterprise.Services.TableReader
```

Or using the .NET CLI:

```shell
dotnet add package Paradigm.Enterprise.Services.TableReader
```

### 1.3.2. Service Registration

Register the table reader service in your ASP.NET Core application's `Startup.cs` or `Program.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<ITableReaderService, TableReaderService>();
}
```

### 1.3.3. Basic Usage Pattern

The typical usage pattern follows these steps:

1. **Create a configuration** - Specify the file type and parsing options
2. **Get a reader instance** - Use the service to create a reader from your data source
3. **Access the schema** - Retrieve column metadata (optional but recommended for performance)
4. **Iterate through rows** - Use `ReadRowAsync()` in a loop to process each row
5. **Extract values** - Use row methods to get typed values from cells

## 1.4. Core Concepts

### 1.4.1. Configuration

Configuration determines how the table reader interprets your data source. The most important setting is `TableReaderType`, which tells the reader which parser to use.

**For CSV files:**

```csharp
var configuration = new TableReaderConfiguration
{
    TableReaderType = TableReaderTypes.Csv,
    CsvParserConfiguration = CsvParserConfiguration.Default
};
```

**For JSON files:**

```csharp
var configuration = new TableReaderConfiguration
{
    TableReaderType = TableReaderTypes.Json
};
```

**For Excel files:**

```csharp
var configuration = new TableReaderConfiguration
{
    TableReaderType = TableReaderTypes.Xls
};
```

**For XML files:**

```csharp
var configuration = new TableReaderConfiguration
{
    TableReaderType = TableReaderTypes.Xml
};
```

### 1.4.2. Schema and Column Access

The schema provides metadata about your table structure. You can access columns by name or index. For better performance and readability, it's recommended to retrieve column references once before the reading loop.

**Using GetRequiredColumn (Recommended):**

```csharp
var firstNameColumn = tableReader.Schema.GetRequiredColumn("first Name");
var lastNameColumn = tableReader.Schema.GetRequiredColumn("last Name");
var ageColumn = tableReader.Schema.GetRequiredColumn("age");

// Later in the loop:
var firstName = row.GetString(firstNameColumn);
var lastName = row.GetString(lastNameColumn);
var age = row.GetInt32(ageColumn);
```

**Using GetColumn (Nullable):**

```csharp
var nameColumn = tableReader.Schema.GetColumn("Name");
if (nameColumn != null)
{
    var name = row.GetString(nameColumn);
}
```

**Using GetColumn by Index:**

```csharp
var firstColumn = tableReader.Schema.GetColumn(0);
var secondColumn = tableReader.Schema.GetColumn(1);
```

### 1.4.3. Reading Rows

The `ReadRowAsync()` method follows a forward-only cursor pattern similar to `IDataReader`. It returns `true` when a row is available and `false` when the end of the data is reached.

```csharp
while (await tableReader.ReadRowAsync())
{
    var row = tableReader.GetCurrentRow();
    // Process the row
}
```

**Important Notes:**

- Always call `GetCurrentRow()` after `ReadRowAsync()` returns `true`
- The reader is forward-only; you cannot rewind or seek backward
- The reader implements `IDisposable`; use `using` statements for proper cleanup

### 1.4.4. Accessing Values

The `IRow` interface provides multiple ways to access cell values:

**1. Using Column Objects (Recommended):**

This approach is type-safe, readable, and performs better because column indices are cached.

```csharp
var nameColumn = schema.GetRequiredColumn("Name");
var ageColumn = schema.GetRequiredColumn("Age");

while (await reader.ReadRowAsync())
{
    var row = reader.GetCurrentRow();
    var name = row.GetString(nameColumn);
    var age = row.GetInt32(ageColumn);
}
```

**2. Using Column Indices:**

Useful when column names are unknown or when working with files without headers.

```csharp
while (await reader.ReadRowAsync())
{
    var row = reader.GetCurrentRow();
    var name = row.GetString(0);  // First column
    var age = row.GetInt32(1);    // Second column
}
```

**3. Using Column Names (Less Efficient):**

The row interface also supports accessing by column name directly, but this is less efficient as it requires a schema lookup for each access.

```csharp
// Note: This pattern is less common in the API
// Prefer using GetRequiredColumn on the schema instead
```

### 1.4.5. Type Conversion

The row interface provides strongly-typed getter methods that handle type conversion automatically. The conversion uses the `Culture` setting from `CsvParserConfiguration` for numeric and date parsing.

**Supported Conversions:**

- String to numeric types (Int32, Int64, Decimal, Double, etc.)
- String to DateTime (respects culture settings)
- String to Boolean
- Null handling (methods return default values or nullable types)

**Example:**

```csharp
var priceColumn = schema.GetRequiredColumn("Price");
var dateColumn = schema.GetRequiredColumn("OrderDate");
var activeColumn = schema.GetRequiredColumn("IsActive");

var price = row.GetDecimal(priceColumn);        // Converts string to decimal
var orderDate = row.GetDateTime(dateColumn);    // Converts string to DateTime
var isActive = row.GetBoolean(activeColumn);     // Converts string to bool
```

## 1.5. Usage Examples

### 1.5.1. Basic CSV Reading Example

This example demonstrates the complete pattern for reading a CSV file and mapping rows to domain objects.

```csharp
/*
CSV Content:
"first Name","last Name","age"
"John","Doe",20
"Jane","Smith",21
"Jim","Beam",22
*/

class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
}

public class PersonProvider
{
    public async Task<List<Person>> GetPeopleAsync(Stream sourceStream)
    {
        // Create a table reader configuration
        var configuration = new TableReaderConfiguration
        {
            TableReaderType = TableReaderTypes.Csv,
            CsvParserConfiguration = CsvParserConfiguration.Default
        };

        // Create a table reader instance for a CSV file
        using var tableReader = this.GetRequestService<ITableReaderService>().GetReaderInstance(sourceStream, true, configuration);

        // Get the required columns from the schema and cache the indices for faster access
        var firstName = tableReader.Schema.GetRequiredColumn("first Name");
        var lastName = tableReader.Schema.GetRequiredColumn("last Name");
        var age = tableReader.Schema.GetRequiredColumn("age");

        var people = new List<Person>();

        // Read the rows from the table reader, this will cycle through the rows and return true until the end of the file is reached
        while (await tableReader.ReadRowAsync())
        {
            // Get the current row
            var row = tableReader.GetCurrentRow();

            // Get the values from the row
            people.Add(new Person
            {
                FirstName = row.GetString(firstName),
                LastName = row.GetString(lastName),
                Age = row.GetInt32(age)
            });
        }

        return people;
    }
}
```

### 1.5.2. Reading from Stream vs Byte Array

The service supports both stream-based and byte array-based input. Streams are preferred for large files as they enable more efficient memory usage.

**Using Stream (Recommended for Large Files):**

```csharp
public async Task<List<Product>> ImportFromStreamAsync(Stream csvStream)
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
        // Process row...
    }

    return products;
}
```

**Using Byte Array:**

```csharp
public async Task<List<Product>> ImportFromBytesAsync(byte[] csvBytes)
{
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Csv,
        CsvParserConfiguration = CsvParserConfiguration.Default
    };

    using var reader = _tableReaderService.GetReaderInstance(csvBytes, true, configuration);
    var products = new List<Product>();

    while (await reader.ReadRowAsync())
    {
        var row = reader.GetCurrentRow();
        // Process row...
    }

    return products;
}
```

### 1.5.3. Reading JSON Files

JSON files are expected to contain an array of objects. The reader treats each object as a row, with object properties as columns.

```csharp
public async Task<List<Order>> ImportOrdersFromJsonAsync(Stream jsonStream)
{
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Json
    };

    using var reader = _tableReaderService.GetReaderInstance(jsonStream, true, configuration);

    var orderIdColumn = reader.Schema.GetRequiredColumn("Id");
    var customerColumn = reader.Schema.GetRequiredColumn("CustomerName");
    var dateColumn = reader.Schema.GetRequiredColumn("OrderDate");
    var amountColumn = reader.Schema.GetRequiredColumn("TotalAmount");

    var orders = new List<Order>();

    while (await reader.ReadRowAsync())
    {
        var row = reader.GetCurrentRow();
        orders.Add(new Order
        {
            Id = row.GetInt32(orderIdColumn),
            CustomerName = row.GetString(customerColumn),
            OrderDate = row.GetDateTime(dateColumn),
            TotalAmount = row.GetDecimal(amountColumn)
        });
    }

    return orders;
}
```

**Example JSON Structure:**

```json
{
  "data": [
    {
      "Id": 1,
      "CustomerName": "John Doe",
      "OrderDate": "2024-01-15",
      "TotalAmount": 125.5
    },
    {
      "Id": 2,
      "CustomerName": "Jane Smith",
      "OrderDate": "2024-01-16",
      "TotalAmount": 89.99
    }
  ]
}
```

### 1.5.4. Reading Excel Files

Excel files (XLS and XLSX) are read similarly to CSV files. The reader automatically detects the worksheet structure.

```csharp
public async Task<List<Employee>> ImportEmployeesFromExcelAsync(Stream excelStream)
{
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Xls
    };

    using var reader = _tableReaderService.GetReaderInstance(excelStream, true, configuration);

    var nameColumn = reader.Schema.GetRequiredColumn("Name");
    var emailColumn = reader.Schema.GetRequiredColumn("Email");
    var departmentColumn = reader.Schema.GetRequiredColumn("Department");
    var salaryColumn = reader.Schema.GetRequiredColumn("Salary");

    var employees = new List<Employee>();

    while (await reader.ReadRowAsync())
    {
        var row = reader.GetCurrentRow();
        employees.Add(new Employee
        {
            Name = row.GetString(nameColumn),
            Email = row.GetString(emailColumn),
            Department = row.GetString(departmentColumn),
            Salary = row.GetDecimal(salaryColumn)
        });
    }

    return employees;
}
```

**Note:** For Excel files, you may need to register the code page provider for older BIFF formats:

```csharp
// In your startup or initialization code
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```

### 1.5.5. Reading XML Files

XML files are parsed with XPath support. The reader expects a specific structure where elements represent rows.

```csharp
public async Task<List<Event>> ImportEventsFromXmlAsync(Stream xmlStream)
{
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Xml
    };

    using var reader = _tableReaderService.GetReaderInstance(xmlStream, true, configuration);

    var eventNameColumn = reader.Schema.GetRequiredColumn("EventName");
    var eventDateColumn = reader.Schema.GetRequiredColumn("EventDate");
    var locationColumn = reader.Schema.GetRequiredColumn("Location");

    var events = new List<Event>();

    while (await reader.ReadRowAsync())
    {
        var row = reader.GetCurrentRow();
        events.Add(new Event
        {
            Name = row.GetString(eventNameColumn),
            Date = row.GetDateTime(eventDateColumn),
            Location = row.GetString(locationColumn)
        });
    }

    return events;
}
```

### 1.5.6. Advanced CSV Configuration

CSV files can have various formats. Use `CsvParserConfiguration` to customize parsing behavior.

**Custom Delimiter (Tab-Separated Values):**

```csharp
var csvConfig = new CsvParserConfiguration
{
    ColumnDelimiter = "\t",
    RowDelimiter = "\n",
    Quotation = '"',
    EscapeCharacter = '\\',
    Culture = CultureInfo.InvariantCulture
};

var configuration = new TableReaderConfiguration
{
    TableReaderType = TableReaderTypes.Csv,
    CsvParserConfiguration = csvConfig
};
```

**Semicolon-Delimited with European Number Format:**

```csharp
var csvConfig = new CsvParserConfiguration
{
    ColumnDelimiter = ";",
    RowDelimiter = "\r\n",
    Quotation = '"',
    EscapeCharacter = '\\',
    Culture = new CultureInfo("de-DE") // German culture for number formatting
};

var configuration = new TableReaderConfiguration
{
    TableReaderType = TableReaderTypes.Csv,
    CsvParserConfiguration = csvConfig
};
```

**Pipe-Delimited File:**

```csharp
var csvConfig = new CsvParserConfiguration
{
    ColumnDelimiter = "|",
    RowDelimiter = "\r\n",
    Quotation = '"',
    EscapeCharacter = '\\',
    Culture = CultureInfo.InvariantCulture
};

var configuration = new TableReaderConfiguration
{
    TableReaderType = TableReaderTypes.Csv,
    CsvParserConfiguration = csvConfig
};
```

### 1.5.7. Handling Files Without Headers

When a file doesn't have a header row, set `sourceHasHeader` to `false`. The reader will generate column names automatically (e.g., "Column0", "Column1", etc.).

```csharp
public async Task<List<DataPoint>> ImportDataWithoutHeadersAsync(Stream csvStream)
{
    var configuration = new TableReaderConfiguration
    {
        TableReaderType = TableReaderTypes.Csv,
        CsvParserConfiguration = CsvParserConfiguration.Default
    };

    // sourceHasHeader = false indicates no header row
    using var reader = _tableReaderService.GetReaderInstance(csvStream, false, configuration);

    // Access columns by index or generated names
    var column0 = reader.Schema.GetColumn(0);  // "Column0"
    var column1 = reader.Schema.GetColumn(1);  // "Column1"
    var column2 = reader.Schema.GetColumn(2);  // "Column2"

    var dataPoints = new List<DataPoint>();

    while (await reader.ReadRowAsync())
    {
        var row = reader.GetCurrentRow();
        dataPoints.Add(new DataPoint
        {
            X = row.GetDouble(0),
            Y = row.GetDouble(1),
            Z = row.GetDouble(2)
        });
    }

    return dataPoints;
}
```

### 1.5.8. Error Handling

The table reader throws exceptions in various scenarios. Implement proper error handling to provide meaningful feedback.

```csharp
public async Task<ImportResult> ImportDataSafelyAsync(Stream sourceStream, TableReaderConfiguration configuration)
{
    var result = new ImportResult();

    try
    {
        using var reader = _tableReaderService.GetReaderInstance(sourceStream, true, configuration);

        var nameColumn = reader.Schema.GetRequiredColumn("Name");
        var ageColumn = reader.Schema.GetRequiredColumn("Age");

        while (await reader.ReadRowAsync())
        {
            try
            {
                var row = reader.GetCurrentRow();

                // Validate required fields
                if (row.IsNull(nameColumn))
                {
                    result.SkippedRows++;
                    result.Errors.Add($"Row {row.Index + 1}: Name is required");
                    continue;
                }

                var person = new Person
                {
                    Name = row.GetString(nameColumn),
                    Age = row.GetInt32(ageColumn)
                };

                result.ImportedRows++;
                result.Data.Add(person);
            }
            catch (Exception ex)
            {
                result.SkippedRows++;
                result.Errors.Add($"Row {reader.GetCurrentRow().Index + 1}: {ex.Message}");
            }
        }
    }
    catch (InvalidOperationException ex)
    {
        // Thrown when GetRequiredColumn fails (column not found)
        result.Errors.Add($"Schema error: {ex.Message}");
    }
    catch (Exception ex)
    {
        result.Errors.Add($"Import failed: {ex.Message}");
    }

    return result;
}
```

### 1.5.9. Performance Considerations

For optimal performance when processing large files:

1. **Cache Column References** - Retrieve column objects once before the loop:

   ```csharp
   // Good: Cache column references
   var nameColumn = schema.GetRequiredColumn("Name");
   while (await reader.ReadRowAsync())
   {
       var name = row.GetString(nameColumn);
   }

   // Less efficient: Looking up by name each time
   while (await reader.ReadRowAsync())
   {
       var name = row.GetString(schema.GetRequiredColumn("Name"));
   }
   ```

2. **Use Column Objects Instead of Indices** - Column objects cache the index lookup:

   ```csharp
   // Good: Using column object
   var name = row.GetString(nameColumn);

   // Also good, but less readable: Using index directly
   var name = row.GetString(0);
   ```

3. **Dispose Readers Properly** - Always use `using` statements:

   ```csharp
   using var reader = _tableReaderService.GetReaderInstance(stream, true, configuration);
   // Reader is automatically disposed
   ```

4. **Process in Batches** - For very large files, consider processing in batches rather than loading everything into memory:

   ```csharp
   const int batchSize = 1000;
   var batch = new List<Person>();

   while (await reader.ReadRowAsync())
   {
       var row = reader.GetCurrentRow();
       batch.Add(new Person { /* ... */ });

       if (batch.Count >= batchSize)
       {
           await ProcessBatchAsync(batch);
           batch.Clear();
       }
   }

   // Process remaining items
   if (batch.Count > 0)
   {
       await ProcessBatchAsync(batch);
   }
   ```

## 1.6. API Reference

### 1.6.1. ITableReaderService

#### GetReaderInstance(Stream, bool, TableReaderConfiguration)

Creates a table reader instance from a stream.

**Parameters:**

- `sourceStream` (Stream) - The input stream containing the table data
- `sourceHasHeader` (bool) - `true` if the first row contains column names; otherwise, `false`
- `configuration` (TableReaderConfiguration) - Configuration specifying file type and parsing options

**Returns:**

- `ITableReader` - A reader instance for the specified format

**Exceptions:**

- `ArgumentNullException` - Thrown when `sourceStream` or `configuration` is `null`
- `Exception` - Thrown when the specified `TableReaderType` is not supported

#### GetReaderInstance(byte[], bool, TableReaderConfiguration)

Creates a table reader instance from a byte array.

**Parameters:**

- `sourceBytes` (byte[]) - The input data as a byte array
- `sourceHasHeader` (bool) - `true` if the first row contains column names; otherwise, `false`
- `configuration` (TableReaderConfiguration) - Configuration specifying file type and parsing options

**Returns:**

- `ITableReader` - A reader instance for the specified format

**Exceptions:**

- `ArgumentNullException` - Thrown when `sourceBytes` or `configuration` is `null`
- `Exception` - Thrown when the specified `TableReaderType` is not supported

### 1.6.2. ITableReader

#### Schema Property

Gets the table schema containing column metadata.

**Type:** `ITableSchema`

**Remarks:**
Access the schema immediately after creating the reader. The schema is populated during reader initialization.

#### ReadRowAsync()

Advances the reader to the next row.

**Returns:**

- `Task<bool>` - `true` if a row is available; `false` if the end of data is reached

**Remarks:**
This method must be called before accessing row data. The first call reads the first data row (after the header, if present).

#### GetCurrentRow()

Gets the current row data.

**Returns:**

- `IRow` - The current row

**Exceptions:**

- `ArgumentNullException` - Thrown when called before `ReadRowAsync()` returns `true`, or after it returns `false`

**Remarks:**
Always call `ReadRowAsync()` first and check that it returns `true` before calling this method.

### 1.6.3. ITableSchema

#### GetRequiredColumn(string)

Gets a column by name, throwing an exception if not found.

**Parameters:**

- `columnName` (string) - The name of the column (case-insensitive)

**Returns:**

- `IColumn` - The column with the specified name

**Exceptions:**

- `InvalidOperationException` - Thrown when the column does not exist

**Remarks:**
Use this method when the column is required. For optional columns, use `GetColumn(string)` instead.

#### GetColumn(string)

Gets a column by name, returning `null` if not found.

**Parameters:**

- `columnName` (string) - The name of the column (case-insensitive)

**Returns:**

- `IColumn?` - The column with the specified name, or `null` if not found

#### GetColumn(int)

Gets a column by index.

**Parameters:**

- `index` (int) - The zero-based column index

**Returns:**

- `IColumn?` - The column at the specified index, or `null` if the index is out of range

### 1.6.4. IRow

#### GetString(IColumn) / GetString(int)

Gets a string value from the specified column.

**Parameters:**

- `column` (IColumn) - The column reference, or
- `index` (int) - The zero-based column index

**Returns:**

- `string?` - The string value, or `null` if the value is null or empty

#### GetInt32(IColumn) / GetInt32(int)

Gets a 32-bit signed integer value from the specified column.

**Parameters:**

- `column` (IColumn) - The column reference, or
- `index` (int) - The zero-based column index

**Returns:**

- `int` - The integer value

**Exceptions:**

- `FormatException` - Thrown when the value cannot be converted to an integer
- `OverflowException` - Thrown when the value is outside the range of `int`

#### GetDecimal(IColumn) / GetDecimal(int)

Gets a decimal value from the specified column.

**Parameters:**

- `column` (IColumn) - The column reference, or
- `index` (int) - The zero-based column index

**Returns:**

- `decimal` - The decimal value

**Exceptions:**

- `FormatException` - Thrown when the value cannot be converted to a decimal
- `OverflowException` - Thrown when the value is outside the range of `decimal`

#### GetDateTime(IColumn) / GetDateTime(int)

Gets a DateTime value from the specified column.

**Parameters:**

- `column` (IColumn) - The column reference, or
- `index` (int) - The zero-based column index

**Returns:**

- `DateTime` - The DateTime value

**Exceptions:**

- `FormatException` - Thrown when the value cannot be converted to a DateTime

**Remarks:**
Date parsing respects the `Culture` setting in `CsvParserConfiguration`.

#### GetBoolean(IColumn) / GetBoolean(int)

Gets a boolean value from the specified column.

**Parameters:**

- `column` (IColumn) - The column reference, or
- `index` (int) - The zero-based column index

**Returns:**

- `bool` - The boolean value

**Remarks:**
Common string representations like "true", "false", "1", "0", "yes", "no" are supported.

#### IsNull(IColumn) / IsNull(int)

Determines whether the value in the specified column is null.

**Parameters:**

- `column` (IColumn) - The column reference, or
- `index` (int) - The zero-based column index

**Returns:**

- `bool` - `true` if the value is null; otherwise, `false`

## 1.7. Best Practices

1. **Always Dispose Readers** - Use `using` statements to ensure proper resource cleanup
2. **Cache Column References** - Retrieve column objects once before the reading loop for better performance
3. **Use GetRequiredColumn for Required Fields** - Let the framework validate schema early
4. **Handle Exceptions Gracefully** - Implement error handling for format conversion and missing columns
5. **Validate Data** - Check for null values and validate business rules during import
6. **Process in Batches** - For large files, process data in batches to manage memory usage
7. **Use Streams for Large Files** - Prefer stream-based input over byte arrays for better memory efficiency
8. **Configure CSV Parsing Appropriately** - Set delimiters, quotation, and culture settings to match your data format

## 1.8. NuGet Package

```shell
Install-Package Paradigm.Enterprise.Services.TableReader
```
