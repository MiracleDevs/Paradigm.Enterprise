using Microsoft.AspNetCore.Http;

namespace Paradigm.Enterprise.WebApi.Middlewares;

public abstract class MiddlewareBase
{
    #region Properties

    /// <summary>
    /// Gets the next.
    /// </summary>
    /// <value>
    /// The next.
    /// </value>
    protected RequestDelegate Next { get; }

    #endregion

    #region Constructor

    protected MiddlewareBase(RequestDelegate next)
    {
        Next = next ?? throw new ArgumentNullException(nameof(next));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Invokes the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public abstract Task Invoke(HttpContext context);

    #endregion
}