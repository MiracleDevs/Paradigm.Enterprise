using Paradigm.Enterprise.Services.TableReader;
using Paradigm.Enterprise.Services.TableReader.Configuration;
using Paradigm.Enterprise.Services.TableReader.Models;
using System.Text;

namespace Paradigm.Enterprise.Tests.Services;

[TestClass]
public class TableWriterServiceTests
{
    private TableWriterService _service = null!;

    // Test data class
    private class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    [TestInitialize]
    public void Setup()
    {
        _service = new TableWriterService();
    }

    #region WriteToStreamAsync Tests

    [TestMethod]
    public async Task WriteToStreamAsync_WithCsvFormat_WritesCorrectCsv()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25, Email = "john@example.com" },
            new Person { Name = "Jane", Age = 30, Email = "jane@example.com" }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString(), p.Email ?? string.Empty],
            ColumnNames = ["Name", "Age", "Email"],
            Configuration = new TableConfiguration
            {
                TableFileType = TableFileTypes.Csv,
                CsvParserConfiguration = CsvParserConfiguration.Default
            }
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("Name,Age,Email", result);
        Assert.Contains("John,25,john@example.com", result);
        Assert.Contains("Jane,30,jane@example.com", result);
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithCsvFormat_WithoutHeader_DoesNotIncludeHeader()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = false,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = ["Name", "Age"]
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.DoesNotContain("Name,Age", result);
        Assert.Contains("John,25", result);
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithCsvFormat_WithCustomDelimiter_UsesCustomDelimiter()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = ["Name", "Age"],
            Configuration = new TableConfiguration
            {
                TableFileType = TableFileTypes.Csv,
                CsvParserConfiguration = new CsvParserConfiguration
                {
                    ColumnDelimiter = ";",
                    RowDelimiter = "\n",
                    Quotation = '"',
                    EscapeCharacter = '\\'
                }
            }
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("Name;Age", result);
        Assert.Contains("John;25", result);
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithCsvFormat_WithQuotedValues_EscapesQuotes()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John \"Johnny\" Doe", Age = 25 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = ["Name", "Age"]
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("\"John \"\"Johnny\"\" Doe\"", result);
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithXlsFormat_WritesCorrectXlsx()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25, Email = "john@example.com" },
            new Person { Name = "Jane", Age = 30, Email = "jane@example.com" }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Xls,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString(), p.Email ?? string.Empty],
            ColumnNames = ["Name", "Age", "Email"]
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        Assert.IsGreaterThan(0, stream.Length);
        // XLSX files start with PK (ZIP file signature)
        var header = new byte[2];
        stream.Read(header, 0, 2);
        Assert.AreEqual(0x50, header[0]); // 'P'
        Assert.AreEqual(0x4B, header[1]); // 'K'
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithXmlFormat_WritesCorrectXml()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25, Email = "john@example.com" },
            new Person { Name = "Jane", Age = 30, Email = "jane@example.com" }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Xml,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString(), p.Email ?? string.Empty],
            ColumnNames = ["Name", "Age", "Email"]
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("<Table>", result);
        Assert.Contains("<Row>", result);
        Assert.Contains("<Name>John</Name>", result);
        Assert.Contains("<Age>25</Age>", result);
        Assert.Contains("<Email>john@example.com</Email>", result);
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithAutoGeneratedColumnNames_GeneratesColumnNames()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = null // Auto-generate
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("Column1,Column2", result);
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithNullValues_HandlesNullsCorrectly()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25, Email = null }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString(), p.Email ?? string.Empty],
            ColumnNames = ["Name", "Age", "Email"]
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("John,25,", result);
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithEmptyData_OnlyWritesHeader()
    {
        // Arrange
        var data = Array.Empty<Person>();

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = ["Name", "Age"]
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("Name,Age", result);
        Assert.DoesNotContain("John", result);
    }

    #endregion

    #region WriteToBytesAsync Tests

    [TestMethod]
    public async Task WriteToBytesAsync_WithCsvFormat_ReturnsCorrectBytes()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = ["Name", "Age"]
        };

        // Act
        var result = await _service.WriteToBytesAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
        var text = Encoding.UTF8.GetString(result);
        Assert.Contains("Name,Age", text);
        Assert.Contains("John,25", text);
    }

    [TestMethod]
    public async Task WriteToBytesAsync_WithXlsFormat_ReturnsValidXlsx()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Xls,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = ["Name", "Age"]
        };

        // Act
        var result = await _service.WriteToBytesAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Length);
        // XLSX files start with PK (ZIP file signature)
        Assert.AreEqual(0x50, result[0]); // 'P'
        Assert.AreEqual(0x4B, result[1]); // 'K'
    }

    [TestMethod]
    public async Task WriteToBytesAsync_WithXmlFormat_ReturnsValidXml()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Xml,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = ["Name", "Age"]
        };

        // Act
        var result = await _service.WriteToBytesAsync(parameters);

        // Assert
        Assert.IsNotNull(result);
        var xml = Encoding.UTF8.GetString(result);
        Assert.Contains("<Table>", xml);
        Assert.Contains("<Name>John</Name>", xml);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task WriteToStreamAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var parameters = new TableWriterParameters<Person>
        {
            Data = Array.Empty<Person>(),
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => Array.Empty<string>()
        };

        // Act & Assert
        try
        {
            await _service.WriteToStreamAsync(null!, parameters);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        try
        {
            await _service.WriteToStreamAsync<Person>(stream, null!);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        var parameters = new TableWriterParameters<Person>
        {
            Data = null!,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => Array.Empty<string>()
        };

        using var stream = new MemoryStream();

        // Act & Assert
        try
        {
            await _service.WriteToStreamAsync<Person>(stream, parameters);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithNullGetColumnValues_ThrowsArgumentNullException()
    {
        // Arrange
        var parameters = new TableWriterParameters<Person>
        {
            Data = Array.Empty<Person>(),
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = null!
        };

        using var stream = new MemoryStream();

        // Act & Assert
        try
        {
            await _service.WriteToStreamAsync(stream, parameters);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithInvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var parameters = new TableWriterParameters<Person>
        {
            Data = Array.Empty<Person>(),
            Format = (TableFileTypes)999, // Invalid enum value
            IncludeHeader = true,
            GetColumnValues = p => Array.Empty<string>()
        };

        using var stream = new MemoryStream();

        // Act & Assert
        try
        {
            await _service.WriteToStreamAsync(stream, parameters);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public async Task WriteToBytesAsync_WithNullParameters_ThrowsArgumentNullException()
    {
        // Act & Assert
        try
        {
            await _service.WriteToBytesAsync<Person>(null!);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // Expected exception
        }
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public async Task WriteToStreamAsync_WithInconsistentColumnCounts_PadsWithEmptyStrings()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25 },
            new Person { Name = "Jane", Age = 30 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Csv,
            IncludeHeader = true,
            GetColumnValues = p => p.Name == "John"
                ? new[] { p.Name, p.Age.ToString() }
                : [p.Name], // Inconsistent column count
            ColumnNames = ["Name", "Age"]
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.HasCount(3, lines); // Header + 2 data rows
        Assert.Contains("Jane", lines[2]); // Second row should have Jane
    }

    [TestMethod]
    public async Task WriteToStreamAsync_WithSpecialCharactersInXml_SanitizesElementNames()
    {
        // Arrange
        var data = new[]
        {
            new Person { Name = "John", Age = 25 }
        };

        var parameters = new TableWriterParameters<Person>
        {
            Data = data,
            Format = TableFileTypes.Xml,
            IncludeHeader = true,
            GetColumnValues = p => [p.Name, p.Age.ToString()],
            ColumnNames = ["Name With Spaces", "Age-Value"] // Invalid XML element names
        };

        using var stream = new MemoryStream();

        // Act
        await _service.WriteToStreamAsync(stream, parameters);

        // Assert
        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        // Should sanitize invalid characters
        Assert.IsTrue(result.Contains("<Name_With_Spaces>") || result.Contains("<NameWithSpaces>"),
            "XML element name should be sanitized");
    }

    #endregion
}

