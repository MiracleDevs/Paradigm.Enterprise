namespace Paradigm.Enterprise.Services.TableReader.Readers;

public interface ITableSchema
{
    /// <summary>
    /// Initializes this instance.
    /// </summary>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns>
    /// The instance.
    /// </returns>
    ITableSchema Initialize(bool sourceHasHeader);

    /// <summary>
    /// Gets the table columns.
    /// </summary>
    /// <returns>A enumeration of columns.</returns>
    IEnumerable<IColumn> GetColumns();

    /// <summary>
    /// Gets the column with the specified name.
    /// </summary>
    /// <param name="columnName">Name of the column.</param>
    /// <returns>A column.</returns>
    IColumn? GetColumn(string columnName);

    /// <summary>
    /// Gets the column with the specified index.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A column.</returns>
    IColumn? GetColumn(int index);

    /// <summary>
    /// Gets the column with the specified name. Throws if the column does not exist.
    /// </summary>
    /// <param name="columnName">Name of the column.</param>
    /// <returns>A column.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the column does not exist.</exception>
    IColumn GetRequiredColumn(string columnName);
}