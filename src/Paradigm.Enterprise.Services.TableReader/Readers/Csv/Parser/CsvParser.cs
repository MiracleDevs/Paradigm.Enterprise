using Paradigm.Enterprise.Services.TableReader.Configuration;
using System.Text;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Csv.Parser;

internal class CsvParser
{
    #region Properties

    /// <summary>
    /// The text reader
    /// </summary>
    private readonly TextReader _textReader;

    /// <summary>
    /// The configuration
    /// </summary>
    private readonly CsvParserConfiguration _configuration;

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    /// <value>
    /// The configuration.
    /// </value>
    public CsvParserConfiguration CsvConfiguration => _configuration;

    /// <summary>
    /// Gets or sets the reading cursor.
    /// </summary>
    /// <value>
    /// The reading cursor.
    /// </value>
    private Cursor ReadingCursor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the end of file was reached.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the end of file was reached; otherwise, <c>false</c>.
    /// </value>
    public bool EndOfFile { get; private set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvParser" /> class.
    /// </summary>
    /// <param name="textReader">The text reader.</param>
    /// <param name="configuration">The configuration.</param>
    public CsvParser(TextReader textReader, CsvParserConfiguration configuration)
    {
        _textReader = textReader;
        _configuration = configuration;
        ReadingCursor = Cursor.Start;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Parses all the rows.
    /// </summary>
    /// <returns>A list of rows containing the cell values.</returns>
    public List<List<string>> ParseAll()
    {
        var rows = new List<List<string>>();

        while (!EndOfFile)
            rows.Add(ParseNextLine());

        return rows;
    }

    /// <summary>
    /// Parses the specified csv text.
    /// </summary>
    /// <returns></returns>
    public List<string> ParseNextLine()
    {
        if (string.IsNullOrEmpty(_configuration.RowDelimiter))
            throw new ArgumentNullException(nameof(_configuration.RowDelimiter));

        if (string.IsNullOrEmpty(_configuration.ColumnDelimiter))
            throw new ArgumentNullException(nameof(_configuration.ColumnDelimiter));

        if (EndOfFile)
            throw new CsvParsingException("End of file reached.", ReadingCursor.Copy());

        var element = string.Empty;
        var results = new List<string>();

        while (true)
        {
            var character = GetNextCharacter();

            if (character == _configuration.RowDelimiter[0] || character == -1)
            {
                ProcessDelimiter(_configuration.RowDelimiter, "row delimiter");
                NextLine();
                results.Add(element);
                return results;
            }

            if (character == _configuration.ColumnDelimiter[0])
            {
                ProcessDelimiter(_configuration.ColumnDelimiter, "column delimiter");
                results.Add(element);
                element = string.Empty;
                continue;
            }

            if (character == _configuration.EscapeCharacter)
            {
                element += ProcessEscape();
                continue;
            }

            if (character == _configuration.Quotation)
            {
                element += ProcessQuotation();
                continue;
            }

            element += (char)character;
        }
    }

    /// <summary>
    /// Moves to start.
    /// </summary>
    public void MoveToStart()
    {
        ReadingCursor = Cursor.Start;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets the next character in the text reader.
    /// </summary>
    /// <returns>A character code.</returns>
    private int GetNextCharacter()
    {
        ReadingCursor.Character++;

        var character = _textReader.Read();

        if (character == -1)
            EndOfFile = true;

        return character;
    }

    /// <summary>
    /// Peeks the next character in the text reader.
    /// </summary>
    /// <returns>A character code.</returns>
    private int Peek()
    {
        return _textReader.Peek();
    }

    /// <summary>
    /// Advance to the next line.
    /// </summary>
    private void NextLine()
    {
        ReadingCursor.Line++;
    }

    /// <summary>
    /// Processes the quotation.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception">The string was not terminated.</exception>
    private string ProcessQuotation()
    {
        var value = string.Empty;
        int character;
        var currentPosition = ReadingCursor.Copy();

        while ((character = GetNextCharacter()) != -1)
        {
            if (character == _configuration.EscapeCharacter)
            {
                value += ProcessEscape();
                continue;
            }

            if (character == _configuration.Quotation)
                return value;

            value += (char)character;
        }

        throw new CsvParsingException("The literal string was not terminated.", currentPosition);
    }

    /// <summary>
    /// Processes the delimiter.
    /// </summary>
    /// <param name="delimiter">The delimiter.</param>
    /// <param name="name">The name.</param>
    /// <exception cref="CsvParsingException">Problems with the file while parsing the {name}: Expecting '{ToLiteral(delimiter)}' but found '{ToLiteral(found)}'.</exception>
    private void ProcessDelimiter(string delimiter, string name)
    {
        if (delimiter.Length == 1)
            return;

        var index = 1;
        int character;
        var found = string.Empty;

        found += delimiter[0];

        while ((character = Peek()) != -1 && index < delimiter.Length)
        {
            found += (char)character;

            if (character != delimiter[index])
                throw new CsvParsingException($"Problems with the file while parsing the {name}: Expecting '{ToLiteral(delimiter)}' but found '{ToLiteral(found)}'.", ReadingCursor.Copy());

            GetNextCharacter();
            index++;
        }
    }

    /// <summary>
    /// Processes the escape.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception">The escaped character is not recognized as valid escapable character.</exception>
    private char ProcessEscape()
    {
        var next = GetNextCharacter();

        switch (next)
        {
            case '\'':
                return '\'';

            case '"':
                return '\"';

            case '\\':
                return '\\';

            case '0':
                return '\0';

            case 'a':
                return '\a';

            case 'b':
                return '\b';

            case 'f':
                return '\f';

            case 'n':
                return '\n';

            case 'r':
                return '\r';

            case 't':
                return '\t';

            case 'v':
                return '\v';

            default:
                throw new CsvParsingException("The escaped character is not recognized as valid escapable character.", ReadingCursor.Copy());
        }
    }

    /// <summary>
    /// Converts to literal.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns></returns>
    private static string ToLiteral(string input)
    {
        var builder = new StringBuilder();

        foreach (var character in input)
            switch (character)
            {
                case '\'':
                    builder.Append("\\'");
                    break;

                case '\"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append("\\\\");
                    break;

                case '\0':
                    builder.Append("\\0");
                    break;

                case '\a':
                    builder.Append("\\a");
                    break;

                case '\b':
                    builder.Append("\\b");
                    break;

                case '\f':
                    builder.Append("\\f");
                    break;

                case '\n':
                    builder.Append("\\n");
                    break;

                case '\r':
                    builder.Append("\\r");
                    break;

                case '\t':
                    builder.Append("\\t");
                    break;

                case '\v':
                    builder.Append("\\v");
                    break;

                default:
                    builder.Append(character);
                    break;
            }

        return builder.ToString();
    }

    #endregion
}