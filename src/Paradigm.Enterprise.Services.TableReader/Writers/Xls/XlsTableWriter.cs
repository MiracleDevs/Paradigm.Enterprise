using ClosedXML.Excel;

namespace Paradigm.Enterprise.Services.TableReader.Writers.Xls;

internal static class XlsTableWriter
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
    public static async Task WriteToStreamAsync<T>(Stream targetStream, IEnumerable<T> data, bool includeHeader, Func<T, IEnumerable<string?>> getColumnValues, IEnumerable<string>? columnNames)
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
            columnCount = columnNamesList.Count;

        // If we still don't have a column count, we can't write anything
        if (columnCount == 0)
            return;

        var workbook = new XLWorkbook();

        try
        {
            var worksheet = workbook.Worksheets.Add("Sheet1");

            var currentRow = 1;

            // Write header if needed
            if (includeHeader && columnNamesList is not null)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    var columnName = columnNamesList[col];
                    worksheet.Cell(currentRow, col + 1).Value = columnName;
                }
                currentRow++;
            }

            // Reset enumerator and write rows
            enumerator = data.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var rowValues = getColumnValues(enumerator.Current).ToList();

                if (rowValues.Count != columnCount)
                    throw new InvalidOperationException("Inconsistent column count in data rows.");

                for (int col = 0; col < columnCount; col++)
                {
                    var cell = worksheet.Cell(currentRow, col + 1);
                    cell.Value = rowValues[col];
                }

                currentRow++;
            }

            // Save the workbook to the stream (synchronous operation)
            // ClosedXML SaveAs will write from the current position
            // For a new stream, position is already 0, but we ensure it's at 0 if seekable
            if (targetStream.CanSeek)
                targetStream.Position = 0;

            workbook.SaveAs(targetStream);

            await targetStream.FlushAsync();
        }
        finally
        {
            workbook.Dispose();
        }

        if (targetStream.CanWrite)
            await targetStream.FlushAsync();
    }

    #endregion
}