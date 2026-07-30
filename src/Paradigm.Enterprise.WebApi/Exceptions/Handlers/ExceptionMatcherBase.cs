using System.Resources;

namespace Paradigm.Enterprise.WebApi.Exceptions.Handlers;

/// <summary>
/// Provides the contract for an exception matcher without prescribing matching or localization logic.
/// </summary>
public abstract class ExceptionMatcherBase : IExceptionMatcher
{
    /// <inheritdoc/>
    public abstract bool Match(Exception ex);

    /// <inheritdoc/>
    public abstract string? GetMessageKey(ResourceManager resourceManager, Exception ex);
}
