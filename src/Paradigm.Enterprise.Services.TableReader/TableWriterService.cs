using Paradigm.Enterprise.Services.TableReader.Configuration;
using Paradigm.Enterprise.Services.TableReader.Models;
using Paradigm.Enterprise.Services.TableReader.Writers.Csv;
using Paradigm.Enterprise.Services.TableReader.Writers.Xls;
using Paradigm.Enterprise.Services.TableReader.Writers.Xml;

namespace Paradigm.Enterprise.Services.TableReader;

public class TableWriterService : ITableWriterService
{
    /// <summary>
    /// Writes data to the specified stream.
    /// </summary>
    /// <typeparam name="T">The type of the data items.</typeparam>
    /// <param name="targetStream">The target stream to write to.</param>
    /// <param name="parameters">The writer parameters.</param>
    /// <returns></returns>
    public async Task WriteToStreamAsync<T>(Stream targetStream, TableWriterParameters<T> parameters)
    {
        if (targetStream is null)
            throw new ArgumentNullException(nameof(targetStream));

        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));

        if (parameters.Data is null)
            throw new ArgumentNullException(nameof(parameters.Data), "Data property cannot be null.");

        if (parameters.GetColumnValues is null)
            throw new ArgumentNullException(nameof(parameters.GetColumnValues), "GetColumnValues property cannot be null.");

        switch (parameters.Format)
        {
            case TableFileTypes.Csv:
                await CsvTableWriter.WriteToStreamAsync(targetStream, parameters.Data, parameters.IncludeHeader, parameters.GetColumnValues, parameters.ColumnNames, parameters.Configuration?.CsvParserConfiguration);
                break;

            case TableFileTypes.Xls:
                await XlsTableWriter.WriteToStreamAsync(targetStream, parameters.Data, parameters.IncludeHeader, parameters.GetColumnValues, parameters.ColumnNames);
                break;

            case TableFileTypes.Xml:
                await XmlTableWriter.WriteToStreamAsync(targetStream, parameters.Data, parameters.IncludeHeader, parameters.GetColumnValues, parameters.ColumnNames);
                break;

            default:
                throw new ArgumentException($"Unsupported file format: {parameters.Format}", nameof(parameters));
        }
    }

    /// <summary>
    /// Writes data and returns as byte array.
    /// </summary>
    /// <typeparam name="T">The type of the data items.</typeparam>
    /// <param name="parameters">The writer parameters.</param>
    /// <returns></returns>
    public async Task<byte[]> WriteToBytesAsync<T>(TableWriterParameters<T> parameters)
    {
        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));

        using var memoryStream = new MemoryStream();
        await WriteToStreamAsync(memoryStream, parameters);

        return memoryStream.ToArray();
    }
}