namespace Paradigm.Enterprise.WebApi.Exceptions.Handlers;

public interface IExceptionHandler
{
    void AddMatcher(IExceptionMatcher matcher);

    Exception Handle(Exception ex);
}