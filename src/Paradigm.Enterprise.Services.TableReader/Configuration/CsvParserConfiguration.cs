namespace Paradigm.Enterprise.Services.TableReader.Configuration;
public class CsvParserConfiguration
{
    /// <summary>
    /// The default options for the csv file.
    /// </summary>
    public static readonly CsvParserConfiguration Default = new() { RowDelimiter = "\r\n", ColumnDelimiter = ",", Quotation = '"', EscapeCharacter = '\\' };

    /// <summary>
    /// Gets the delimiter.
    /// </summary>
    /// <value>
    /// The delimiter.
    /// </value>
    public string? ColumnDelimiter { get; set; }

    /// <summary>
    /// Gets the delimiter.
    /// </summary>
    /// <value>
    /// The delimiter.
    /// </value>
    public string? RowDelimiter { get; set; }

    /// <summary>
    /// Gets or sets the quotation.
    /// </summary>
    /// <value>
    /// The quotation.
    /// </value>
    public char? Quotation { get; set; }

    /// <summary>
    /// Gets or sets the escape characters for string values.
    /// </summary>
    /// <value>
    /// The quotation.
    /// </value>
    public char? EscapeCharacter { get; set; }
}
