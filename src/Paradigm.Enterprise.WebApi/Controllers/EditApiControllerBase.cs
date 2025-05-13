using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Providers;

namespace Paradigm.Enterprise.WebApi.Controllers;

[AllowAnonymous]
[ApiController]
public abstract class EditApiControllerBase<TProvider, TView, TParameters> : ReadApiControllerBase<TProvider, TView, TParameters>
    where TProvider : IEditProvider<TView>
    where TView : EntityBase, new()
    where TParameters : PaginationParametersBase
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EditApiControllerBase{TProvider, TView, TParameters}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    protected EditApiControllerBase(ILogger<ReadApiControllerBase<TProvider, TView, TParameters>> logger, TProvider provider) : base(logger, provider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Saves the entity.
    /// </summary>
    /// <param name="view">The view.</param>
    /// <returns></returns>
    [HttpPost]
    //[ExposeEndpoint]
    public virtual async Task<TView> SaveAsync([FromBody] TView view)
    {
        return await Provider.SaveAsync(view);
    }

    /// <summary>
    /// Deletes the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    [HttpDelete]
    //[ExposeEndpoint]
    public virtual async Task DeleteAsync([FromQuery] int id)
    {
        await Provider.DeleteAsync(id);
    }

    #endregion
}
