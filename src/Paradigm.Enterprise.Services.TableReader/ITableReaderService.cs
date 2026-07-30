using Paradigm.Enterprise.Services.Core;
using Paradigm.Enterprise.Services.TableReader.Configuration;
using Paradigm.Enterprise.Services.TableReader.Readers;

namespace Paradigm.Enterprise.Services.TableReader;

/// <summary>
/// Creates forward-only table readers for supported stream and byte-array sources.
/// </summary>
/// <example>
/// Read a CSV stream sequentially and dispose the reader before disposing the caller-owned stream:
/// <code>
/// using var input = File.OpenRead("products.csv");
/// using var reader = tableReaderService.GetReaderInstance(
///     input,
///     sourceHasHeader: true,
///     configuration: new TableConfiguration { TableFileType = TableFileTypes.Csv });
///
/// while (await reader.ReadRowAsync())
/// {
///     var productName = reader.GetCurrentRow().GetString(0);
/// }
/// </code>
/// </example>
public interface ITableReaderService : IService
{
    /// <summary>
    /// Gets the reader instance.
    /// </summary>
    /// <param name="sourceStream">The readable source stream. The caller retains ownership.</param>
    /// <param name="sourceHasHeader"><see langword="true"/> when the first record contains column names.</param>
    /// <param name="configuration">The format and format-specific parser settings.</param>
    /// <returns>A format-specific reader positioned before its first row.</returns>
    /// <remarks>
    /// CSV and XLS readers retain the source and require it to remain open while the reader is used.
    /// JSON and XML readers fully load the source during this call, so it may be closed afterward.
    /// Disposing any returned reader leaves the caller-owned stream open.
    /// </remarks>
    /// <exception cref="Exception"><see cref="TableConfiguration.TableFileType"/> is not supported.</exception>
    ITableReader GetReaderInstance(Stream sourceStream, bool sourceHasHeader, TableConfiguration configuration);

    /// <summary>
    /// Gets the reader instance.
    /// </summary>
    /// <param name="sourceBytes">The complete table document.</param>
    /// <param name="sourceHasHeader"><see langword="true"/> when the first record contains column names.</param>
    /// <param name="configuration">The format and format-specific parser settings.</param>
    /// <returns>A format-specific reader positioned before its first row.</returns>
    /// <exception cref="Exception"><see cref="TableConfiguration.TableFileType"/> is not supported.</exception>
    ITableReader GetReaderInstance(byte[] sourceBytes, bool sourceHasHeader, TableConfiguration configuration);
}
