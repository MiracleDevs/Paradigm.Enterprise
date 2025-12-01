using Paradigm.Enterprise.Services.TableReader.Configuration;
using System.Text;

namespace Paradigm.Enterprise.Services.TableReader.Writers.Csv;

internal static class CsvTableWriter
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
    /// <param name="configuration">The configuration.</param>
    public static async Task WriteToStreamAsync<T>(Stream targetStream, IEnumerable<T> data, bool includeHeader, Func<T, IEnumerable<string>> getColumnValues, IEnumerable<string>? columnNames, CsvParserConfiguration? configuration)
    {
        var config = configuration ?? CsvParserConfiguration.Default;
        var columnDelimiter = config.ColumnDelimiter ?? ",";
        var rowDelimiter = config.RowDelimiter ?? "\r\n";
        var quote = config.Quotation ?? '"';
        var escape = config.EscapeCharacter ?? '\\';

        using var writer = new StreamWriter(targetStream, Encoding.UTF8, leaveOpen: true);

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

            // Write header if needed
            if (includeHeader && columnNamesList is not null)
                await WriteRowAsync(writer, columnNamesList, columnDelimiter, rowDelimiter, quote, escape);

            // Write first row
            await WriteRowAsync(writer, firstRowValues, columnDelimiter, rowDelimiter, quote, escape);

            // Write remaining rows
            while (enumerator.MoveNext())
            {
                var rowValues = getColumnValues(enumerator.Current).ToList();

                // Ensure consistent column count
                while (rowValues.Count < columnCount)
                {
                    rowValues.Add(string.Empty);
                }

                await WriteRowAsync(writer, rowValues.Take(columnCount), columnDelimiter, rowDelimiter, quote, escape);
            }
        }
        else if (includeHeader && columnNamesList is not null)
        {
            // Empty data but header requested
            await WriteRowAsync(writer, columnNamesList, columnDelimiter, rowDelimiter, quote, escape);
        }

        await writer.FlushAsync();
    }


    #endregion

    #region Private Methods

    /// <summary>
    /// Writes a single row to the stream.
    /// </summary>
    private static async Task WriteRowAsync(StreamWriter writer, IEnumerable<string> values, string columnDelimiter, string rowDelimiter, char quote, char escape)
    {
        var first = true;

        foreach (var value in values)
        {
            if (!first)
                await writer.WriteAsync(columnDelimiter);

            first = false;

            var escapedValue = EscapeCsvValue(value ?? string.Empty, columnDelimiter, rowDelimiter, quote, escape);
            await writer.WriteAsync(escapedValue);
        }

        await writer.WriteAsync(rowDelimiter);
    }

    /// <summary>
    /// Escapes a CSV value according to the configuration.
    /// </summary>
    private static string EscapeCsvValue(string value, string columnDelimiter, string rowDelimiter, char quote, char escape)
    {
        // Check if value needs quoting (contains delimiter, quote, newline, or row delimiter)
        var needsQuoting = value.Contains(columnDelimiter) ||
                          value.Contains(quote) ||
                          value.Contains('\n') ||
                          value.Contains('\r') ||
                          value.Contains(rowDelimiter);

        if (!needsQuoting)
            return value;

        // Escape quotes and escape characters within the value
        var escaped = new StringBuilder();
        escaped.Append(quote);

        foreach (var ch in value)
        {
            if (ch == quote)
            {
                // Escape quote by doubling it (standard CSV behavior)
                escaped.Append(quote);
                escaped.Append(quote);
            }
            else if (ch == escape)
            {
                // Escape escape character
                escaped.Append(escape);
                escaped.Append(escape);
            }
            else
            {
                escaped.Append(ch);
            }
        }

        escaped.Append(quote);
        return escaped.ToString();
    }

    #endregion
}

