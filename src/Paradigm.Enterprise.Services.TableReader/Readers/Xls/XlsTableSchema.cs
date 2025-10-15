using ExcelDataReader;
using Paradigm.Enterprise.Services.TableReader.Readers.Base;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Xls;

internal class XlsTableSchema : TableSchemaBase
{
    #region Properties

    /// <summary>
    /// Gets the data reader.
    /// </summary>
    /// <value>
    /// The data reader.
    /// </value>
    private IExcelDataReader DataReader { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="XlsTableSchema"/> class.
    /// </summary>
    /// <param name="dataReader">The data reader.</param>
    public XlsTableSchema(IExcelDataReader dataReader)
    {
        DataReader = dataReader;
    }

    #endregion

    #region Abstract implementation

    /// <summary>
    /// Gets the table schema columns.
    /// </summary>
    /// <returns></returns>
    protected override IList<IColumn> GetTableSchemaColumns()
    {
        var columnsList = new List<XlsColumn>();

        DataReader.Read();

        GenerateColumnsNames(DataReader.FieldCount);

        for (var i = 0; i < DataReader.FieldCount; i++)
        {
            var columnName = SourceHasHeader ? DataReader.GetString(i) : GetGeneratedColumnName(i);
            columnsList.Add(new XlsColumn(i, columnName, typeof(string)));
        }

        if (!SourceHasHeader)
            DataReader.Reset();

        return columnsList.Cast<IColumn>().ToList();
    }

    #endregion
}