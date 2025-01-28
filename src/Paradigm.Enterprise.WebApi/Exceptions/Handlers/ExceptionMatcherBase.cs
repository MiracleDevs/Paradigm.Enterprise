using System.Resources;

namespace Paradigm.Enterprise.WebApi.Exceptions.Handlers;

public abstract class ExceptionMatcherBase : IExceptionMatcher
{
    public abstract bool Match(Exception ex);

    public abstract string? GetMessageKey(ResourceManager resourceManager, Exception ex);
}