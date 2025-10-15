using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using Paradigm.Enterprise.Services.TableReader.Readers.Csv.Parser;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Csv;

internal class CsvTableSchema : TableSchemaBase
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
    /// Initializes a new instance of the <see cref="CsvTableSchema"/> class.
    /// </summary>
    /// <param name="parser">The parser.</param>
    public CsvTableSchema(CsvParser parser)
    {
        Parser = parser;
    }

    #endregion

    #region Abstract implementation

    /// <summary>
    /// Gets the table schema columns.
    /// </summary>
    /// <returns></returns>
    protected override IList<IColumn> GetTableSchemaColumns()
    {
        var firstRowFields = Parser.ParseNextLine();

        GenerateColumnsNames(firstRowFields.Count);

        var columnsList = firstRowFields
            .Select((x, i) =>
            {
                var columnName = SourceHasHeader ? x : GetGeneratedColumnName(i);
                return new CsvColumn(i, columnName, typeof(string));
            })
            .Cast<IColumn>()
            .ToList();

        if (!SourceHasHeader)
            Parser.MoveToStart();

        return columnsList;
    }

    #endregion
}