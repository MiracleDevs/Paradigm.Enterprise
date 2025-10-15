namespace Paradigm.Enterprise.Services.TableReader.Readers.Base;

/// <summary>
/// Represents an error that occur when the values fo not follow the schema.
/// </summary>
/// <seealso cref="Exception" />
public class TableSchemaException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvSchemaException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TableSchemaException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvSchemaException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
    public TableSchemaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}