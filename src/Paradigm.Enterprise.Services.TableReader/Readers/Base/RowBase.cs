namespace Paradigm.Enterprise.Services.TableReader.Readers.Base;

internal abstract class RowBase : IRow
{
    #region Properties

    /// <summary>
    /// Gets the table schema.
    /// </summary>
    /// <value>
    /// The table schema.
    /// </value>
    protected ITableSchema TableSchema { get; }

    /// <inheritdoc/>
    public int Index { get; protected set; }

    /// <summary>
    /// Gets the scalar values.
    /// </summary>
    /// <value>
    /// The values.
    /// </value>
    protected List<string> Values { get; set; }

    /// <inheritdoc/>
    public object? this[int index]
    {
        get
        {
            if (index < 0 || index >= Values.Count)
                throw new IndexOutOfRangeException($"The index of the column '{index}' it's out of range for the row number {Index}.");

            return Values[index];
        }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RowBase"/> class.
    /// </summary>
    /// <param name="tableSchema">The table schema.</param>
    /// <exception cref="ArgumentNullException">tableSchema</exception>
    internal RowBase(ITableSchema tableSchema)
    {
        TableSchema = tableSchema ?? throw new ArgumentNullException(nameof(tableSchema));
        Values = new List<string>();
    }

    #endregion

    #region Abstract Methods

    /// <inheritdoc/>
    public abstract bool Read();

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public virtual object? GetValue(int index) => this[index];

    /// <inheritdoc/>
    public virtual bool IsNull(int index) => this[index] is null || this[index] == DBNull.Value;

    /// <inheritdoc/>
    public virtual byte GetByte(int index) => Convert.ToByte(this[index]);

    /// <inheritdoc/>
    public virtual sbyte GetSByte(int index) => Convert.ToSByte(this[index]);

    /// <inheritdoc/>
    public virtual ushort GetUInt16(int index) => Convert.ToUInt16(this[index]);

    /// <inheritdoc/>
    public virtual short GetInt16(int index) => Convert.ToInt16(this[index]);

    /// <inheritdoc/>
    public virtual uint GetUInt32(int index) => Convert.ToUInt32(this[index]);

    /// <inheritdoc/>
    public virtual int GetInt32(int index) => Convert.ToInt32(this[index]);

    /// <inheritdoc/>
    public virtual ulong GetUInt64(int index) => Convert.ToUInt64(this[index]);

    /// <inheritdoc/>
    public virtual long GetInt64(int index) => Convert.ToInt64(this[index]);

    /// <inheritdoc/>
    public virtual float GetSingle(int index) => Convert.ToSingle(this[index]);

    /// <inheritdoc/>
    public virtual double GetDouble(int index) => Convert.ToDouble(this[index]);

    /// <inheritdoc/>
    public virtual decimal GetDecimal(int index) => Convert.ToDecimal(this[index]);

    /// <inheritdoc/>
    public virtual char GetChar(int index) => Convert.ToChar(this[index]);

    /// <inheritdoc/>
    public virtual string? GetString(int index) => Convert.ToString(this[index]);

    /// <inheritdoc/>
    public virtual DateTime GetDateTime(int index) => Convert.ToDateTime(this[index]);

    /// <inheritdoc/>
    public virtual bool GetBoolean(int index) => Convert.ToBoolean(this[index]);

    /// <inheritdoc/>
    public object? GetValue(IColumn column) => GetValue(column.Index);

    /// <inheritdoc/>
    public bool IsNull(IColumn column) => IsNull(column.Index);

    /// <inheritdoc/>
    public byte GetByte(IColumn column) => GetByte(column.Index);

    /// <inheritdoc/>
    public sbyte GetSByte(IColumn column) => GetSByte(column.Index);

    /// <inheritdoc/>
    public ushort GetUInt16(IColumn column) => GetUInt16(column.Index);

    /// <inheritdoc/>
    public short GetInt16(IColumn column) => GetInt16(column.Index);

    /// <inheritdoc/>
    public uint GetUInt32(IColumn column) => GetUInt32(column.Index);

    /// <inheritdoc/>
    public int GetInt32(IColumn column) => GetInt32(column.Index);

    /// <inheritdoc/>
    public ulong GetUInt64(IColumn column) => GetUInt64(column.Index);

    /// <inheritdoc/>
    public long GetInt64(IColumn column) => GetInt64(column.Index);

    /// <inheritdoc/>
    public float GetSingle(IColumn column) => GetSingle(column.Index);

    /// <inheritdoc/>
    public double GetDouble(IColumn column) => GetDouble(column.Index);

    /// <inheritdoc/>
    public decimal GetDecimal(IColumn column) => GetDecimal(column.Index);

    /// <inheritdoc/>
    public char GetChar(IColumn column) => GetChar(column.Index);

    /// <inheritdoc/>
    public string? GetString(IColumn column) => GetString(column.Index);

    /// <inheritdoc/>
    public DateTime GetDateTime(IColumn column) => GetDateTime(column.Index);

    /// <inheritdoc/>
    public bool GetBoolean(IColumn column) => GetBoolean(column.Index);

    #endregion

    #region Protected Methods

    /// <summary>
    /// Validates the values schema.
    /// </summary>
    /// <exception cref="TableSchemaException">The row number {this.Index} does not have the proper amount of columns. The file has {this.TableSchema.GetColumns().Count()} but the row has {this.Values.Count}.</exception>
    protected void ValidateValuesSchema()
    {
        if (Values.Count > TableSchema.GetColumns().Count())
            throw new TableSchemaException($"The row number {Index} does not have the proper amount of columns. The file has {TableSchema.GetColumns().Count()} but the row has {Values.Count}.");
    }

    #endregion
}
