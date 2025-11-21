namespace Paradigm.Enterprise.Services.TableReader.Configuration;

public class TableReaderConfiguration
{
    /// <summary>
    /// Gets the type of the table reader.
    /// </summary>
    /// <value>
    /// The type of the table reader.
    /// </value>
    public TableReaderTypes TableReaderType { get; set; }

    /// <summary>
    /// Gets or sets the CSV parser configuration.
    /// </summary>
    /// <value>
    /// The CSV parser configuration.
    /// </value>
    public CsvParserConfiguration? CsvParserConfiguration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [indent results].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [indent results]; otherwise, <c>false</c>.
    /// </value>
    public bool IndentResults { get; set; }
}