namespace Paradigm.Enterprise.Services.TableReader.Readers;

public interface ITableReader : IDisposable
{
    /// <summary>
    /// Gets or sets the table schema.
    /// </summary>
    /// <value>
    /// The schema.
    /// </value>
    ITableSchema? Schema { get; }

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