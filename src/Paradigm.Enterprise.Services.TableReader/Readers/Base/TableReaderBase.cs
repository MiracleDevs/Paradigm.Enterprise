namespace Paradigm.Enterprise.Services.TableReader.Readers.Base;

internal abstract class TableReaderBase : ITableReader
{
    #region Properties

    /// <summary>
    /// Gets or sets the table schema.
    /// </summary>
    /// <value>
    /// The schema.
    /// </value>
    public ITableSchema Schema { get; protected set; } = null!;

    /// <summary>
    /// Gets or sets the current row.
    /// </summary>
    /// <value>
    /// The current row.
    /// </value>
    protected IRow? CurrentRow { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads a new row of data.
    /// </summary>
    /// <returns>
    /// A row of data.
    /// </returns>
    public virtual Task<bool> ReadRowAsync()
    {
        if (CurrentRow is null)
            throw new ArgumentNullException(nameof(CurrentRow));

        return Task.FromResult(CurrentRow.Read());
    }

    /// <summary>
    /// Gets the current row.
    /// </summary>
    /// <returns>
    /// Current row
    /// </returns>
    public IRow GetCurrentRow() => CurrentRow ?? throw new ArgumentNullException(nameof(CurrentRow));

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public virtual void Dispose()
    {
    }

    #endregion
}