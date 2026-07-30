namespace Paradigm.Enterprise.Domain.Exceptions;

/// <summary>
/// Represents one or more violations of domain rules.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DomainException(string message) : base(message)
    {

    }
}
