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