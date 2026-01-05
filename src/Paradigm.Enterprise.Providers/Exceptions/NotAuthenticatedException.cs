namespace Paradigm.Enterprise.Providers.Exceptions;

public class NotAuthenticatedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotAuthenticatedException"/> class.
    /// </summary>
    public NotAuthenticatedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotAuthenticatedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public NotAuthenticatedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotAuthenticatedException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public NotAuthenticatedException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}