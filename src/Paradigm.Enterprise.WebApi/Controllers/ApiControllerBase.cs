using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Providers;
using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.WebApi.Controllers;

[AllowAnonymous]
[ApiController]
public abstract class ApiControllerBase<TProvider> : ControllerBase

    where TProvider : IProvider
{
    #region Properties

    /// <summary>
    /// Gets the logger.
    /// </summary>
    /// <value>
    /// The logger.
    /// </value>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the provider.
    /// </summary>
    /// <value>
    /// The provider.
    /// </value>
    protected TProvider Provider { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiControllerBase{TProvider}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    public ApiControllerBase(ILogger<ApiControllerBase<TProvider>> logger, TProvider provider)
    {
        Logger = logger;
        Provider = provider;
    }

    #endregion
}

[AllowAnonymous]
[ApiController]
public abstract class ApiControllerBase<TProvider, TView> : ApiControllerBase<TProvider, TView, FilterTextPaginatedParameters>
    where TProvider : IReadProvider<TView, FilterTextPaginatedParameters>
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiControllerBase{TProvider, TView}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    public ApiControllerBase(ILogger<ApiControllerBase<TProvider, TView, FilterTextPaginatedParameters>> logger, TProvider provider) : base(logger, provider)
    {
    }

    #endregion
}

[AllowAnonymous]
[ApiController]
public abstract class ApiControllerBase<TProvider, TView, TParameters> : ControllerBase
    where TProvider : IReadProvider<TView, TParameters>
    where TParameters : FilterTextPaginatedParameters
{
    #region Properties

    /// <summary>
    /// Gets the logger.
    /// </summary>
    /// <value>
    /// The logger.
    /// </value>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the provider.
    /// </summary>
    /// <value>
    /// The provider.
    /// </value>
    protected TProvider Provider { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiControllerBase{TProvider, TView, TParameters}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    public ApiControllerBase(ILogger<ApiControllerBase<TProvider, TView, TParameters>> logger, TProvider provider)
    {
        Logger = logger;
        Provider = provider;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Searches the asynchronous.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    [HttpPost("search")]
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync([FromBody, Required] TParameters parameters)
    {
        return await Provider.SearchPaginatedAsync(parameters);
    }

    #endregion
}