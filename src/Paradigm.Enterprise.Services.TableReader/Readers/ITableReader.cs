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
    /// Gets or sets the table schema.
    /// </summary>
    /// <value>
    /// The schema.
    /// </value>
    ITableSchema Schema { get; }

    /// <summary>
    /// Reads a new row of data.
    /// </summary>
    /// <returns>A row of data.</returns>
    Task<bool> ReadRowAsync();

    /// <summary>
    /// Gets the current row.
    /// </summary>
    /// <returns>Current row</returns>
    IRow GetCurrentRow();
}
