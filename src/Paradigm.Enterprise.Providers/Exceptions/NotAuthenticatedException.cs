namespace Paradigm.Enterprise.Providers.Exceptions;

public class NotAuthenticatedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotAuthorizedException"/> class.
    /// </summary>
    public NotAuthenticatedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotAuthorizedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public NotAuthenticatedException(string message) : base(message)
    {
    }
}