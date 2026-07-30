using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Paradigm.Enterprise.WebApi.Controllers;

/// <summary>
/// Provides logging support for Paradigm API controllers.
/// </summary>
/// <remarks>
/// This base class is decorated with <see cref="AllowAnonymousAttribute"/>. Derived controllers
/// therefore permit anonymous requests unless they apply and enforce a separate authorization
/// mechanism, such as <see cref="Paradigm.Enterprise.WebApi.Attributes.ApiAuthorizationAttribute"/>.
/// Endpoint exposure and request authorization are independent concerns.
/// </remarks>
/// <example>
/// A protected controller opts into both endpoint exposure and API-key authorization:
/// <code>
/// [Route("api/status")]
/// [ApiAuthorization]
/// public sealed class StatusController : ApiControllerBase
/// {
///     public StatusController(ILogger&lt;ApiControllerBase&gt; logger)
///         : base(logger)
///     {
///     }
///
///     [HttpGet]
///     [ExposeEndpoint]
///     public IActionResult Get() => Ok(new { Status = "Healthy" });
/// }
/// </code>
/// Configure <c>ClientSecrets</c> and add endpoint exposure control before using this pattern.
/// </example>
[AllowAnonymous]
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    #region Properties

    /// <summary>
    /// Gets the logger.
    /// </summary>
    /// <value>
    /// The logger.
    /// </value>
    protected ILogger Logger { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiControllerBase"/> class.
    /// </summary>
    /// <param name="logger">The logger retained for use by the controller and derived types.</param>
    public ApiControllerBase(ILogger<ApiControllerBase> logger)
    {
        Logger = logger;
    }

    #endregion
}

/// <summary>
/// Provides logging and a provider dependency for Paradigm API controllers.
/// </summary>
/// <typeparam name="TProvider">The application provider used by the controller.</typeparam>
/// <remarks>
/// The anonymous-access behavior inherited from <see cref="ApiControllerBase"/> also applies to
/// this class. Secure derived controllers explicitly with the application's authorization policy.
/// </remarks>
public abstract class ApiControllerBase<TProvider> : ApiControllerBase
{
    #region Properties

    /// <summary>
    /// Gets the provider.
    /// </summary>
    protected TProvider Provider { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiControllerBase{TProvider}"/> class.
    /// </summary>
    /// <param name="logger">The logger retained by the base controller.</param>
    /// <param name="provider">The provider retained for derived actions.</param>
    protected ApiControllerBase(ILogger<ApiControllerBase> logger, TProvider provider)
        : base(logger)
    {
        Provider = provider;
    }

    #endregion
}
