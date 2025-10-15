namespace Paradigm.Enterprise.Services.TableReader.Readers.Csv.Parser;

/// <summary>
/// Represents an error that occur during the parsing of a csv file.
/// </summary>
/// <seealso cref="Exception" />
public class CsvParsingException : Exception
{
    /// <summary>
    /// Gets the cursor.
    /// </summary>
    /// <value>
    /// The cursor.
    /// </value>
    public Cursor Cursor { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvParsingException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="cursor">The cursor.</param>
    public CsvParsingException(string message, Cursor cursor) : base($"[Line: {cursor.Line} Character: {cursor.Character}]: {message}")
    {
        Cursor = cursor;
    }
}