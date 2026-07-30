namespace Paradigm.Enterprise.Services.TableReader.Readers;

/// <summary>
/// Reads a table sequentially and exposes its schema and current row.
/// </summary>
/// <remarks>
/// Dispose the reader after use to release parser resources. A reader created from a caller-provided
/// stream leaves that stream open; the caller controls the stream lifetime.
/// </remarks>
public interface ITableReader : IDisposable
{
    /// <summary>
    /// Gets the table schema.
    /// </summary>
    /// <value>
    /// The schema.
    /// </value>
    ITableSchema Schema { get; }

    /// <summary>
    /// Advances to and processes the next row.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when a non-empty row was processed; <see langword="false"/> at the
    /// end of the source or when the underlying row reports itself empty.
    /// </returns>
    Task<bool> ReadRowAsync();

    /// <summary>
    /// Gets the accessor for the current row.
    /// </summary>
    /// <returns>The current row accessor.</returns>
    IRow GetCurrentRow();
}
