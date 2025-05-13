﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Providers;
using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.WebApi.Controllers;

[AllowAnonymous]
[ApiController]
public abstract class ReadApiControllerBase<TProvider, TView, TParameters> : ApiControllerBase
    where TProvider : IReadProvider<TView>
    where TParameters : PaginationParametersBase
{

    #region Properties

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
    /// Initializes a new instance of the <see cref="ReadApiControllerBase{TProvider, TView, TParameters}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    protected ReadApiControllerBase(ILogger<ApiControllerBase> logger, TProvider provider) : base(logger)
    {
        Provider = provider;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    [HttpPost("search")]
    //[ExposeEndpoint] // New attribute to control endpoint exposure
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync([FromBody, Required] TParameters parameters)
    {
        return await Provider.SearchAsync(parameters);
    }

    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
    [HttpGet("get-by-id")]
    //[ExposeEndpoint]
    public virtual async Task<TView> GetByIdAsync([FromQuery] int id)
    {
        return await Provider.GetByIdAsync(id);
    }

    #endregion
}