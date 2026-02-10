using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Providers.Prueba;
using Paradigm.Enterprise.WebApi.Attributes;
using Paradigm.Enterprise.WebApi.Controllers.Prueba;

namespace Paradigm.Enterprise.WebApi.Controllers;

[AllowAnonymous]
[ApiController]
public abstract class EntitypiControllerBase<TProvider, TEntity, TView, TParameters> : EntityReadApiControllerBase<TProvider, TEntity, TView, TParameters>
    where TProvider : IEntityProvider<TEntity, TView>
    where TEntity : EntityBase
    where TView : EntityBase
    where TParameters : PaginationParametersBase
{
    protected EntitypiControllerBase(ILogger<ApiControllerBase> logger, TProvider provider) : base(logger, provider)
    {
    }

    #region Public Methods

    /// <summary>
    /// Saves the entity.
    /// </summary>
    /// <param name="view">The view.</param>
    /// <returns></returns>
    [HttpPost]
    [ExposeEndpoint]
    public virtual async Task<TView> SaveAsync([FromBody] TView view)
    {
        return await Provider.SaveAsync(view);
    }

    /// <summary>
    /// Deletes the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    [HttpDelete]
    [ExposeEndpoint]
    public virtual async Task DeleteAsync([FromQuery] int id)
    {
        await Provider.DeleteAsync(id);
    }

    #endregion
}
