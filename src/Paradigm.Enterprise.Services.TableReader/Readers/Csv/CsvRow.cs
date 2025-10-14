using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using Paradigm.Enterprise.Services.TableReader.Readers.Csv.Parser;

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
}