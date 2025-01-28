namespace Paradigm.Enterprise.Providers.Exceptions;

public class NotAuthorizedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotAuthorizedException"/> class.
    /// </summary>
    public NotAuthorizedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotAuthorizedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public NotAuthorizedException(string message) : base(message)
    {
    }
}