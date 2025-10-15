namespace Paradigm.Enterprise.Services.TableReader.Readers.Base;

internal abstract class TableSchemaBase : ITableSchema
{
    #region Properties

    /// <summary>
    /// Gets the table columns.
    /// </summary>
    /// <value>
    /// The columns.
    /// </value>
    protected IList<IColumn>? Columns { get; private set; }

    /// <summary>
    /// Gets a value indicating whether [source has header].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [source has header]; otherwise, <c>false</c>.
    /// </value>
    protected bool SourceHasHeader { get; private set; }

    /// <summary>
    /// Gets the generated column names.
    /// </summary>
    /// <value>
    /// The generated column names.
    /// </value>
    private List<string>? GeneratedColumnNames { get; set; }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Gets the table schema columns.
    /// </summary>
    /// <returns></returns>
    protected abstract IList<IColumn> GetTableSchemaColumns();

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes this instance.
    /// </summary>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns>
    /// The instance.
    /// </returns>
    public ITableSchema Initialize(bool sourceHasHeader)
    {
        SourceHasHeader = sourceHasHeader;
        Columns = GetTableSchemaColumns();
        return this;
    }

    /// <summary>
    /// Gets the table columns.
    /// </summary>
    /// <returns>
    /// A enumeration of columns.
    /// </returns>
    public IEnumerable<IColumn> GetColumns() => Columns ?? Array.Empty<IColumn>();

    /// <summary>
    /// Gets the column with the specified name.
    /// </summary>
    /// <param name="columnName">Name of the column.</param>
    /// <returns>
    /// A column.
    /// </returns>
    public IColumn? GetColumn(string columnName) => Columns?.SingleOrDefault(x => x.Name?.Equals(columnName, StringComparison.InvariantCultureIgnoreCase) ?? false);

    /// <summary>
    /// Gets the column with the specified index.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A column.
    /// </returns>
    public IColumn? GetColumn(int index) => Columns?.SingleOrDefault(x => x.Index == index);

    #endregion

    #region Protected Methods

    /// <summary>
    /// Generates the columns names when source has no header.
    /// </summary>
    /// <param name="fieldsCount">The fields count.</param>
    protected void GenerateColumnsNames(int fieldsCount)
    {
        var columnsNames = new List<string>();

        if (!SourceHasHeader)
        {
            const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            for (var i = 0; i < fieldsCount; i++)
            {
                var retVal = string.Empty;
                var value = i;
                int remainder;

                do
                {
                    remainder = value % 26;
                    retVal = string.Concat(CHARS.Substring(remainder, 1), retVal);
                    value = value / 26 - 1;
                } while (value + 1 > 0);

                columnsNames.Add(retVal);
            }
        }

        GeneratedColumnNames = columnsNames;
    }

    /// <summary>
    /// Gets the name of the generated column.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns></returns>
    protected string GetGeneratedColumnName(int index)
    {
        if (GeneratedColumnNames is null)
            throw new ArgumentNullException(nameof(GeneratedColumnNames));

        return GeneratedColumnNames[index];
    }

    #endregion
}