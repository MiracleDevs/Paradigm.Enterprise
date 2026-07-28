# Read and write table files

`Paradigm.Enterprise.Services.TableReader` presents CSV, Excel, JSON, and XML input as a schema and a forward-only sequence of rows. The same package writes CSV, Excel, and XML output through `ITableWriterService`.

## Read a file

Choose the file type explicitly and keep the returned reader in a `using` statement.

```csharp
var configuration = new TableConfiguration
{
    TableFileType = TableFileTypes.Csv,
    CsvParserConfiguration = new CsvParserConfiguration
    {
        ColumnDelimiter = ","
    }
};

using var reader = tableReaderService.GetReaderInstance(
    sourceStream,
    sourceHasHeader: true,
    configuration);

var nameColumn = reader.Schema.GetRequiredColumn("Name");

while (await reader.ReadRowAsync())
{
    var row = reader.GetCurrentRow();
    var name = row.GetString(nameColumn);
}
```

The schema is available after the reader is created. `GetRequiredColumn` throws a schema exception when a column is missing; `GetColumn` is appropriate when the column is optional.

Rows support access by numeric position or `IColumn`, null checks, and typed conversion methods. A conversion can fail when source data does not match the requested type, so include row and column context in application-level error reporting.

## Configuration

`TableConfiguration.TableFileType` selects `Csv`, `Xls`, `Json`, or `Xml`. CSV options control delimiter and related reader and writer behavior. `XmlParserConfiguration` currently affects only XML writing, including indentation, encoding, and whether to omit the declaration. The XML reader does not consume that configuration. `TableConfiguration.IndentResults` is present in the public model but is not used by the current readers or writers.

The `Xls` value selects the current Excel reader or writer. JSON is supported for reading but not writing.

## Write a file

The writer receives typed data and a function that extracts column values.

```csharp
var parameters = new TableWriterParameters<CatalogItemView>
{
    Data = items,
    Format = TableFileTypes.Csv,
    IncludeHeader = true,
    ColumnNames = ["Id", "Name"],
    GetColumnValues = item => [item.Id.ToString(), item.Name]
};

await tableWriterService.WriteToStreamAsync(targetStream, parameters);
```

`WriteToBytesAsync` is convenient for small responses. `WriteToStreamAsync` avoids returning an additional full byte array, but the current implementation rewinds the destination with `Seek(0, SeekOrigin.Begin)` after writing. The destination must therefore be writable and seekable. For an HTTP response body or another non-seekable destination, write to a seekable buffer and then copy that buffer to the final stream.

## Stream ownership

Treat stream lifetime as part of the caller's contract. Dispose the table reader. Keep input and output streams open only as long as the surrounding HTTP, blob, or file operation needs them.

Validate file size and content before parsing untrusted uploads. File extension and content type alone do not establish that input is safe or well formed.
