namespace Paradigm.Enterprise.Services.TableReader.Readers.Csv.Parser;

public class Cursor
{
    /// <summary>
    /// Gets the start.
    /// </summary>
    /// <value>
    /// The start.
    /// </value>
    public static Cursor Start => new(1, 1);

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    /// <value>
    /// The line number.
    /// </value>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the character number.
    /// </summary>
    /// <value>
    /// The character number.
    /// </value>
    public int Character { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Cursor"/> class.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="character">The character.</param>
    public Cursor(int line, int character)
    {
        Line = line;
        Character = character;
    }

    /// <summary>
    /// Copies this instance.
    /// </summary>
    /// <returns></returns>
    public Cursor Copy()
    {
        return new Cursor(Line, Character);
    }
}