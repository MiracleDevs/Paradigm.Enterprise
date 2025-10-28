using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using Paradigm.Enterprise.Services.TableReader.Readers.Csv.Parser;
using System.Globalization;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Csv;

internal class CsvRow : RowBase
{
    #region Properties

    /// <summary>
    /// Gets the parser.
    /// </summary>
    /// <value>
    /// The parser.
    /// </value>
    private CsvParser Parser { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvRow" /> class.
    /// </summary>
    /// <param name="tableSchema">The table schema.</param>
    /// <param name="parser">The parser.</param>
    public CsvRow(ITableSchema tableSchema, CsvParser parser) : base(tableSchema)
    {
        Parser = parser;
    }

    #endregion

    #region Abstract implementation

    /// <summary>
    /// Reads and new row.
    /// </summary>
    /// <returns></returns>
    public override bool Read()
    {
        if (Parser.EndOfFile)
            return false;

        Values = Parser.ParseNextLine();
        Index++;

        if (Values.Count == 0 || Values.Count == 1 && string.IsNullOrEmpty(Values[0]))
            return false;

        ValidateValuesSchema();

        return true;
    }

    #endregion

    #region Conversion Overrides

    /// <summary>
    /// Gets a byte value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A byte value.</returns>
    public override byte GetByte(int index) => Convert.ToByte(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a sbyte value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A sbyte value.</returns>
    public override sbyte GetSByte(int index) => Convert.ToSByte(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a ushort value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A ushort value.</returns>
    public override ushort GetUInt16(int index) => Convert.ToUInt16(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a short value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A short value.</returns>
    public override short GetInt16(int index) => Convert.ToInt16(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a uint value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A uint value.</returns>
    public override uint GetUInt32(int index) => Convert.ToUInt32(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a int value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A int value.</returns>
    public override int GetInt32(int index) => Convert.ToInt32(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a ulong value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A ulong value.</returns>
    public override ulong GetUInt64(int index) => Convert.ToUInt64(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a long value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A long value.</returns>
    public override long GetInt64(int index) => Convert.ToInt64(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a float value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A float value.</returns>
    public override float GetSingle(int index) => Convert.ToSingle(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a double value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A double value.</returns>
    public override double GetDouble(int index) => Convert.ToDouble(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a decimal value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A decimal value.</returns>
    public override decimal GetDecimal(int index) => Convert.ToDecimal(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a DateTime value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A DateTime value.</returns>
    public override DateTime GetDateTime(int index) => Convert.ToDateTime(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets a boolean value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A boolean value.</returns>
    public override bool GetBoolean(int index) => Convert.ToBoolean(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    #endregion
}