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
    /// <returns><see langword="true"/> when a row was read; otherwise <see langword="false"/>.</returns>
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

    /// <inheritdoc/>
    public override byte GetByte(int index) => Convert.ToByte(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override sbyte GetSByte(int index) => Convert.ToSByte(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override ushort GetUInt16(int index) => Convert.ToUInt16(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override short GetInt16(int index) => Convert.ToInt16(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override uint GetUInt32(int index) => Convert.ToUInt32(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override int GetInt32(int index) => Convert.ToInt32(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override ulong GetUInt64(int index) => Convert.ToUInt64(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override long GetInt64(int index) => Convert.ToInt64(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override float GetSingle(int index) => Convert.ToSingle(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override double GetDouble(int index) => Convert.ToDouble(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override decimal GetDecimal(int index) => Convert.ToDecimal(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override DateTime GetDateTime(int index) => Convert.ToDateTime(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override bool GetBoolean(int index) => Convert.ToBoolean(this[index], Parser.CsvConfiguration.Culture ?? CultureInfo.InvariantCulture);

    #endregion
}
