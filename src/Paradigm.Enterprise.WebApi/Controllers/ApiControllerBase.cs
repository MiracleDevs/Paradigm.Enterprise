using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Paradigm.Enterprise.WebApi.Controllers;

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