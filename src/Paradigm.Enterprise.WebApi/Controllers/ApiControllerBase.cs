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
    /// <param name="logger">The logger.</param>
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
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    protected ApiControllerBase(ILogger<ApiControllerBase> logger, TProvider provider)
        : base(logger)
    {
        Provider = provider;
    }

    #endregion
}
