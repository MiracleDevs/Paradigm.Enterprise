using System.Globalization;

namespace Paradigm.Enterprise.Services.TableReader.Configuration;

/// <summary>
/// Configures delimiters, quoting, escaping, and value conversion for CSV input and output.
/// </summary>
public class CsvParserConfiguration
{
    /// <summary>
    /// The default options for the csv file.
    /// </summary>
    public static readonly CsvParserConfiguration Default = new() { RowDelimiter = "\r\n", ColumnDelimiter = ",", Quotation = '"', EscapeCharacter = '\\', Culture = CultureInfo.InvariantCulture };

    /// <summary>
    /// Gets or sets the text separating adjacent columns.
    /// </summary>
    /// <value>
    /// The column delimiter, or <see langword="null"/> to use the parser default.
    /// </value>
    public string? ColumnDelimiter { get; set; }

    /// <summary>
    /// Gets or sets the text separating records.
    /// </summary>
    /// <value>
    /// The row delimiter, or <see langword="null"/> to use the parser default.
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

    /// <summary>
    /// Gets or sets the culture for numeric and date conversions.
    /// Defaults to InvariantCulture for consistent parsing across locales.
    /// </summary>
    /// <value>
    /// The culture.
    /// </value>
    public IFormatProvider? Culture { get; set; }
}
