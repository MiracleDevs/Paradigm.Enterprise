using Paradigm.Enterprise.Services.Core;
using Paradigm.Enterprise.Services.TableReader.Models;

namespace Paradigm.Enterprise.Services.TableReader;

/// <summary>
/// Serializes object sequences to supported tabular formats.
/// </summary>
/// <example>
/// Write selected values and an explicit header to a caller-owned stream:
/// <code>
/// using var output = new MemoryStream();
/// await tableWriterService.WriteToStreamAsync(
///     output,
///     new TableWriterParameters&lt;Product&gt;
///     {
///         Data = products,
///         Format = TableFileTypes.Csv,
///         IncludeHeader = true,
///         ColumnNames = ["Name", "Price"],
///         GetColumnValues = product => [product.Name, product.Price.ToString()]
///     });
/// </code>
/// </example>
public interface ITableWriterService : IService
{
    /// <summary>
    /// Writes data to the specified stream.
    /// </summary>
    /// <typeparam name="T">The type of the data items.</typeparam>
    /// <param name="targetStream">The writable, seekable target stream to write to and rewind.</param>
    /// <param name="parameters">The writer parameters.</param>
    /// <returns>A task that completes after the document is written and the stream is rewound.</returns>
    /// <remarks>The service does not dispose <paramref name="targetStream"/>.</remarks>
    Task WriteToStreamAsync<T>(Stream targetStream, TableWriterParameters<T> parameters);

    /// <summary>
    /// Writes data and returns as byte array.
    /// </summary>
    /// <typeparam name="T">The type of the data items.</typeparam>
    /// <param name="parameters">The writer parameters.</param>
    /// <returns>The complete serialized table document.</returns>
    Task<byte[]> WriteToBytesAsync<T>(TableWriterParameters<T> parameters);
}

