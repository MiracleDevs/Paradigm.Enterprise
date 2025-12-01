using System.Text;
using System.Xml;

namespace Paradigm.Enterprise.Services.TableReader.Writers.Xml;

internal static class XmlTableWriter
{
    #region Public Methods

    /// <summary>
    /// Writes data to the specified stream.
    /// </summary>
    /// <typeparam name="T">The type of the data items.</typeparam>
    /// <param name="targetStream">The target stream.</param>
    /// <param name="data">The data to write.</param>
    /// <param name="includeHeader">if set to <c>true</c> [include header].</param>
    /// <param name="getColumnValues">The delegate function that extracts column values from each item.</param>
    /// <param name="columnNames">The column names.</param>
    public static async Task WriteToStreamAsync<T>(Stream targetStream, IEnumerable<T> data, bool includeHeader, Func<T, IEnumerable<string>> getColumnValues, IEnumerable<string>? columnNames)
    {
        // Determine column count and names
        var columnCount = 0;
        var columnNamesList = columnNames?.ToList();

        // Get first item to determine column count
        var enumerator = data.GetEnumerator();
        if (enumerator.MoveNext())
        {
            var firstRowValues = getColumnValues(enumerator.Current).ToList();
            columnCount = firstRowValues.Count;

            // Generate column names if needed
            if (includeHeader && columnNamesList is null)
            {
                columnNamesList = new List<string>();
                for (int i = 0; i < columnCount; i++)
                {
                    columnNamesList.Add($"Column{i + 1}");
                }
            }
        }
        else if (includeHeader && columnNamesList is not null)
        {
            columnCount = columnNamesList.Count;
        }

        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var writer = XmlWriter.Create(targetStream, settings);

        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "Table", null);

        // Reset enumerator and write rows
        enumerator = data.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var rowValues = getColumnValues(enumerator.Current).ToList();

            // Ensure consistent column count
            while (rowValues.Count < columnCount)
            {
                rowValues.Add(string.Empty);
            }

            await writer.WriteStartElementAsync(null, "Row", null);

            for (int i = 0; i < columnCount; i++)
            {
                var columnName = SanitizeXmlName(columnNamesList?[i] ?? $"Column{i + 1}");
                var value = i < rowValues.Count ? (rowValues[i] ?? string.Empty) : string.Empty;

                await writer.WriteStartElementAsync(null, columnName, null);
                await writer.WriteStringAsync(value);
                await writer.WriteEndElementAsync();
            }

            await writer.WriteEndElementAsync(); // Row
        }

        await writer.WriteEndElementAsync(); // Table
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Sanitizes a string to be a valid XML element name.
    /// </summary>
    private static string SanitizeXmlName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Column";

        // XML element names must start with a letter or underscore
        var sanitized = new StringBuilder();
        var firstChar = name[0];

        if (char.IsLetter(firstChar) || firstChar == '_')
            sanitized.Append(firstChar);
        else
            sanitized.Append('_');

        // Remaining characters can be letters, digits, hyphens, periods, or underscores
        for (int i = 1; i < name.Length; i++)
        {
            var ch = name[i];
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '.' || ch == '_')
                sanitized.Append(ch);
            else
                sanitized.Append('_');
        }

        return sanitized.ToString();
    }

    #endregion
}

