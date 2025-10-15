using Paradigm.Enterprise.Services.TableReader.Configuration;
using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using Paradigm.Enterprise.Services.TableReader.Readers.Csv.Parser;
using System.Text;
using System.Text.RegularExpressions;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Csv;

internal class CsvTableReader : TableReaderBase
{
    #region Properties

    /// <summary>
    /// Gets or sets the parser.
    /// </summary>
    /// <value>
    /// The parser.
    /// </value>
    private CsvParser Parser { get; }

    /// <summary>
    /// Gets or sets the text reader.
    /// </summary>
    /// <value>
    /// The text reader.
    /// </value>
    private TextReader TextReader { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvTableReader" /> class.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <param name="configuration">The configuration.</param>
    private CsvTableReader(byte[] content, bool sourceHasHeader, CsvParserConfiguration configuration)
    {
        TextReader = new StringReader(Encoding.UTF8.GetString(content));
        Parser = new CsvParser(TextReader, configuration);
        Schema = new CsvTableSchema(Parser).Initialize(sourceHasHeader);
        CurrentRow = new CsvRow(Schema, Parser);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Opens a new table reader from the specified content.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    public static ITableReader OpenFromContent(byte[] content, bool sourceHasHeader, CsvParserConfiguration? configuration = null)
    {
        var defaultConfiguration = CsvParserConfiguration.Default;

        configuration = configuration ?? defaultConfiguration;
        configuration.ColumnDelimiter = !string.IsNullOrWhiteSpace(configuration.ColumnDelimiter) ? Regex.Unescape(configuration.ColumnDelimiter) : defaultConfiguration.ColumnDelimiter;
        configuration.EscapeCharacter = configuration.EscapeCharacter != char.MinValue ? configuration.EscapeCharacter : defaultConfiguration.EscapeCharacter;
        configuration.Quotation = configuration.Quotation != char.MinValue ? configuration.Quotation : defaultConfiguration.Quotation;
        configuration.RowDelimiter = !string.IsNullOrWhiteSpace(configuration.RowDelimiter) ? Regex.Unescape(configuration.RowDelimiter) : defaultConfiguration.RowDelimiter;
        return new CsvTableReader(content, sourceHasHeader, configuration);
    }

    /// <summary>
    /// Reads a new row of data.
    /// </summary>
    /// <returns>
    /// A row of data.
    /// </returns>
    /// <exception cref="Exception">Can not read a row when the reader is closed.</exception>
    public override Task<bool> ReadRowAsync()
    {
        if (TextReader is null)
            throw new Exception("Can not read a row when the reader is closed.");

        return base.ReadRowAsync();
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public override void Dispose()
    {
        TextReader?.Dispose();
    }

    #endregion
}