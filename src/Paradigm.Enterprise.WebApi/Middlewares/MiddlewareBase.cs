using Microsoft.AspNetCore.Http;

namespace Paradigm.Enterprise.WebApi.Middlewares;

/// <summary>
/// Provides the next request delegate for custom ASP.NET Core middleware.
/// </summary>
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

    /// <summary>
    /// Initializes the middleware with the next component in the pipeline.
    /// </summary>
    /// <param name="next">The next request delegate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    protected MiddlewareBase(RequestDelegate next)
    {
        Next = next ?? throw new ArgumentNullException(nameof(next));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Invokes the specified context.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that represents processing the request.</returns>
    public abstract Task Invoke(HttpContext context);

    #endregion
}
