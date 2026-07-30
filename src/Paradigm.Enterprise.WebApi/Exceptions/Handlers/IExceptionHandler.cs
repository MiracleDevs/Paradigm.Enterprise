namespace Paradigm.Enterprise.WebApi.Exceptions.Handlers;

/// <summary>
/// Translates exceptions, including wrapped exceptions, using an ordered set of matchers.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Appends a matcher to the translation order.
    /// </summary>
    /// <param name="matcher">The matcher to evaluate after previously registered matchers.</param>
    void AddMatcher(IExceptionMatcher matcher);

    /// <summary>
    /// Attempts to translate an exception or one of its inner exceptions.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>The translated exception when a matcher succeeds; otherwise <paramref name="ex"/>.</returns>
    Exception Handle(Exception ex);
}
