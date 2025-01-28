using System.Resources;

namespace Paradigm.Enterprise.WebApi.Exceptions.Handlers;

public interface IExceptionMatcher
{
    bool Match(Exception ex);

    string? GetMessageKey(ResourceManager resourceManager, Exception ex);
}