namespace Paradigm.Enterprise.Services.TableReader.Configuration;

public class TableConfiguration
{
    /// <summary>
    /// Gets the type of the table file.
    /// </summary>
    /// <value>
    /// The type of the table file.
    /// </value>
    public TableFileTypes TableFileType { get; set; }

    /// <summary>
    /// Gets or sets the CSV parser configuration.
    /// </summary>
    /// <value>
    /// The CSV parser configuration.
    /// </value>
    public CsvParserConfiguration? CsvParserConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the XML parser configuration.
    /// </summary>
    /// <value>
    /// The XML parser configuration.
    /// </value>
    public XmlParserConfiguration? XmlParserConfiguration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [indent results].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [indent results]; otherwise, <c>false</c>.
    /// </value>
    public bool IndentResults { get; set; }
}