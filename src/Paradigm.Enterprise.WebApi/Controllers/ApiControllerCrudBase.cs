using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Providers;

namespace Paradigm.Enterprise.WebApi.Controllers;

[AllowAnonymous]
public abstract class ApiControllerCrudBase<TProvider, TView, TParameters> : ApiControllerBase<TProvider, TView, TParameters>
    where TView : EntityBase, new()
    where TProvider : IEditProvider<TView>
    where TParameters : FilterTextPaginatedParameters
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiControllerCrudBase{TProvider, TView}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    public ApiControllerCrudBase(ILogger<ApiControllerCrudBase<TProvider, TView, TParameters>> logger, TProvider provider) : base(logger, provider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the by identifier asynchronous.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
    [HttpGet("get-by-id")]
    public virtual async Task<TView> GetByIdAsync([FromQuery] int id)
    {
        return await Provider.GetByIdAsync(id);
    }

    /// <summary>
    /// Saves the asynchronous.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<TView> SaveAsync([FromBody] TView dto)
    {
        return await Provider.SaveAsync(dto);
    }

    #endregion
}