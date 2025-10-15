using ExcelDataReader;
using Paradigm.Enterprise.Services.TableReader.Readers.Base;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Xls;

internal class XlsRow : RowBase
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
    /// Initializes a new instance of the <see cref="XlsRow" /> class.
    /// </summary>
    /// <param name="tableSchema">The table schema.</param>
    /// <param name="dataReader">The data reader.</param>
    public XlsRow(ITableSchema tableSchema, IExcelDataReader dataReader) : base(tableSchema)
    {
        DataReader = dataReader;
    }

    #endregion

    #region Abstract implementations

    /// <summary>
    /// Reads a new row.
    /// </summary>
    /// <returns></returns>
    public override bool Read()
    {
        if (!DataReader.Read())
            return false;

        FillValues();
        Index++;

        if (Values.Count == 0 || Values.Count == 1 && string.IsNullOrEmpty(Values[0]))
            return false;

        ValidateValuesSchema();

        return true;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Fills the values from the source file.
    /// </summary>
    private void FillValues()
    {
        Values = [];

        for (var i = 0; i < DataReader.FieldCount; i++)
            Values.Add(DataReader.GetValue(i)?.ToString() ?? string.Empty);
    }

    #endregion
}