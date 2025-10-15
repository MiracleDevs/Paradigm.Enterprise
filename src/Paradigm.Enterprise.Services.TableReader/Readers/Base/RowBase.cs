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

    /// <summary>
    /// Gets the row index.
    /// </summary>
    /// <value>
    /// The row index.
    /// </value>
    public int Index { get; protected set; }

    /// <summary>
    /// Gets the scalar values.
    /// </summary>
    /// <value>
    /// The values.
    /// </value>
    protected List<string> Values { get; set; }

    /// <summary>
    /// Gets the <see cref="object"/> with the specified column name.
    /// </summary>
    /// <value>
    /// The <see cref="object"/>.
    /// </value>
    /// <param name="index">Index of the column.</param>
    /// <returns></returns>
    /// <exception cref="Exception">The index of the column <see cref="index"/> it's out of range for the row number N.</exception>
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

    /// <summary>
    /// Reads a new row.
    /// </summary>
    /// <returns></returns>
    public abstract bool Read();

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets a object value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A object value.
    /// </returns>
    public object? GetValue(int index) => this[index];

    /// <summary>
    /// Determines whether the value specified by the column name is null.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// <c>true</c> if the value is null; otherwise, <c>false</c>.
    /// </returns>
    public bool IsNull(int index) => this[index] is null || this[index] == DBNull.Value;

    /// <summary>
    /// Gets a byte value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A byte value.
    /// </returns>
    public byte GetByte(int index) => Convert.ToByte(this[index]);

    /// <summary>
    /// Gets a sbyte value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A sbyte value.
    /// </returns>
    public sbyte GetSByte(int index) => Convert.ToSByte(this[index]);

    /// <summary>
    /// Gets a ushort value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A ushort value.
    /// </returns>
    public ushort GetUInt16(int index) => Convert.ToUInt16(this[index]);

    /// <summary>
    /// Gets a short value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A short value.
    /// </returns>
    public short GetInt16(int index) => Convert.ToInt16(this[index]);

    /// <summary>
    /// Gets a uint value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A uint value.
    /// </returns>
    public uint GetUInt32(int index) => Convert.ToUInt32(this[index]);

    /// <summary>
    /// Gets a int value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A int value.
    /// </returns>
    public int GetInt32(int index) => Convert.ToInt32(this[index]);

    /// <summary>
    /// Gets a ulong value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A ulong value.
    /// </returns>
    public ulong GetUInt64(int index) => Convert.ToUInt64(this[index]);

    /// <summary>
    /// Gets a long value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A long value.
    /// </returns>
    public long GetInt64(int index) => Convert.ToInt64(this[index]);

    /// <summary>
    /// Gets a float value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A float value.
    /// </returns>
    public float GetSingle(int index) => Convert.ToSingle(this[index]);

    /// <summary>
    /// Gets a double value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A double value.
    /// </returns>
    public double GetDouble(int index) => Convert.ToDouble(this[index]);

    /// <summary>
    /// Gets a decimal value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A decimal value.
    /// </returns>
    public decimal GetDecimal(int index) => Convert.ToDecimal(this[index]);

    /// <summary>
    /// Gets a char value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A char value.
    /// </returns>
    public char GetChar(int index) => Convert.ToChar(this[index]);

    /// <summary>
    /// Gets a string value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A string value.
    /// </returns>
    public string? GetString(int index) => Convert.ToString(this[index]);

    /// <summary>
    /// Gets a DateTime value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A DateTime value.
    /// </returns>
    public DateTime GetDateTime(int index) => Convert.ToDateTime(this[index]);

    /// <summary>
    /// Gets a boolean value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    /// A boolean value.
    /// </returns>
    public bool GetBoolean(int index) => Convert.ToBoolean(this[index]);

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