using System.Resources;

namespace Paradigm.Enterprise.WebApi.Exceptions.Handlers;

public class ExceptionHandler : IExceptionHandler
{
    private readonly List<IExceptionMatcher> _matchers;

    private readonly ResourceManager _resourceManager;

    public ExceptionHandler(Type resourceType)
    {
        _resourceManager = new ResourceManager(resourceType);
        _matchers = [];
    }

    public void AddMatcher(IExceptionMatcher matcher)
    {
        _matchers.Add(matcher);
    }

    public Exception Handle(Exception ex)
    {
        if (ex is not AggregateException collectionException)
            return ProcessException(ex) ?? ex;

        foreach (var exception in collectionException.InnerExceptions)
        {
            var result = ProcessException(exception);

            if (result is not null)
                return result;
        }

        return ex;
    }

    private Exception? ProcessException(Exception ex)
    {
        foreach (var matcher in _matchers)
        {
            var e = ex;

            while (e is not null)
            {
                if (!matcher.Match(e))
                {
                    e = e.InnerException;
                    continue;
                }

                var message = matcher.GetMessageKey(_resourceManager, e);
                
                if (message is not null)
                {
                    var exceptionType = e.GetType();
                    var constructor = exceptionType.GetConstructor([typeof(string), typeof(Exception)]);
                    if (constructor is not null)
                        return (Exception)constructor.Invoke([message, e]);
                    
                    var stringConstructor = exceptionType.GetConstructor([typeof(string)]);
                    if (stringConstructor is not null)
                        return (Exception)stringConstructor.Invoke([message]);
                }

                return e;
            }
        }

        return null;
    }
}