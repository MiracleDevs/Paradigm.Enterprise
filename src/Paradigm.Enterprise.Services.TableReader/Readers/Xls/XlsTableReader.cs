using ExcelDataReader;
using Paradigm.Enterprise.Services.TableReader.Readers.Base;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Xls;

internal class XlsTableReader : TableReaderBase
{
    #region Porperties

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
    /// Initializes a new instance of the <see cref="XlsTableReader" /> class.
    /// </summary>
    /// <param name="contentStream">The content stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    private XlsTableReader(Stream contentStream, bool sourceHasHeader)
    {
        DataReader = ExcelReaderFactory.CreateReader(contentStream);
        Schema = new XlsTableSchema(DataReader).Initialize(sourceHasHeader);
        CurrentRow = new XlsRow(Schema, DataReader);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Opens a new table reader from the specified content.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns></returns>
    public static ITableReader OpenFromContent(byte[] content, bool sourceHasHeader)
    {
        return new XlsTableReader(new MemoryStream(content), sourceHasHeader);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public override void Dispose()
    {
        DataReader?.Dispose();
    }

    #endregion
}