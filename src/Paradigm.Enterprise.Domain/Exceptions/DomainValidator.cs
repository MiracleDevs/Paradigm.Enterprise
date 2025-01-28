using System.Text;

namespace Paradigm.Enterprise.Domain.Exceptions;

public class DomainValidator
{
    /// <summary>
    /// Gets or sets the message builder.
    /// </summary>
    /// <value>
    /// The message builder.
    /// </value>
    private StringBuilder MessageBuilder { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    public DomainValidator()
    {
        MessageBuilder = new StringBuilder();
    }

    /// <summary>
    /// Adds a new error to the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public void AddError(string message)
    {
        MessageBuilder.AppendLine(message);
    }

    /// <summary>
    /// Asserts the specified condition.
    /// </summary>
    /// <param name="condition">if set to <c>true</c> [condition].</param>
    /// <param name="message">The error message if the condition fails.</param>
    public void Assert(bool condition, string message)
    {
        if (!condition)
            AddError(message);
    }

    /// <summary>
    /// Throws if errors were found.
    /// </summary>
    /// <exception cref="DomainException"></exception>
    public void ThrowIfAny()
    {
        if (MessageBuilder.Length != 0)
            throw new DomainException(MessageBuilder.ToString());
    }
}