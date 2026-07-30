using System.Resources;

namespace Paradigm.Enterprise.WebApi.Exceptions.Handlers;

/// <summary>
/// Identifies exceptions that can be translated to localized messages.
/// </summary>
public interface IExceptionMatcher
{
    /// <summary>
    /// Determines whether this matcher handles an exception.
    /// </summary>
    /// <param name="ex">The exception under consideration.</param>
    /// <returns><see langword="true"/> when the matcher can handle the exception.</returns>
    bool Match(Exception ex);

    /// <summary>
    /// Resolves the translated message for a matched exception.
    /// </summary>
    /// <param name="resourceManager">The resource manager configured by the handler.</param>
    /// <param name="ex">The matched exception.</param>
    /// <returns>The translated message, or <see langword="null"/> to retain the original exception.</returns>
    string? GetMessageKey(ResourceManager resourceManager, Exception ex);
}
