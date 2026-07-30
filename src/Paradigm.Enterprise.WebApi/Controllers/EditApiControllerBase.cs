using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Providers;
using Paradigm.Enterprise.WebApi.Attributes;

namespace Paradigm.Enterprise.WebApi.Controllers;

/// <summary>
/// Adds conventional save and delete actions to a read API controller.
/// </summary>
/// <typeparam name="TProvider">The edit provider that performs persistence operations.</typeparam>
/// <typeparam name="TView">The entity view accepted and returned by the controller.</typeparam>
/// <typeparam name="TParameters">The pagination parameters accepted by inherited searches.</typeparam>
/// <typeparam name="TId">The value-type entity identifier.</typeparam>
/// <remarks>
/// The inherited and declared actions allow anonymous access. Standard <c>[Authorize]</c> metadata
/// on a derived controller does not override the inherited <see cref="AllowAnonymousAttribute"/>.
/// Protected mutations require an independently enforced mechanism such as
/// <see cref="ApiAuthorizationAttribute"/>, or a different base controller that does not allow
/// anonymous access.
/// </remarks>
/// <example>
/// A derived controller exposes the inherited search, lookup, save, and delete routes:
/// <code>
/// [Route("api/products")]
/// [ApiAuthorization]
/// public sealed class ProductsController
///     : EditApiControllerBase&lt;IProductProvider, ProductView, ProductSearch, int&gt;
/// {
///     public ProductsController(
///         ILogger&lt;ReadApiControllerBase&lt;IProductProvider, ProductView, ProductSearch, int&gt;&gt; logger,
///         IProductProvider provider)
///         : base(logger, provider)
///     {
///     }
/// }
/// </code>
/// In this example, <c>IProductProvider</c> implements <c>IEditProvider&lt;ProductView, int&gt;</c>,
/// <c>ProductView</c> derives from <c>EntityBase&lt;int&gt;</c>, and <c>ProductSearch</c> derives
/// from <see cref="PaginationParametersBase"/>.
/// </example>
[AllowAnonymous]
[ApiController]
public abstract class EditApiControllerBase<TProvider, TView, TParameters, TId> : ReadApiControllerBase<TProvider, TView, TParameters, TId>
    where TId : struct, IEquatable<TId>
    where TProvider : IEditProvider<TView, TId>
    where TView : EntityBase<TId>, new()
    where TParameters : PaginationParametersBase
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="EditApiControllerBase{TProvider, TView, TParameters, TId}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    protected EditApiControllerBase(ILogger<ReadApiControllerBase<TProvider, TView, TParameters, TId>> logger, TProvider provider) : base(logger, provider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Saves the entity.
    /// </summary>
    /// <param name="view">The entity state to create or update.</param>
    /// <returns>The persisted view returned by the provider.</returns>
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
    public virtual async Task DeleteAsync([FromQuery] TId id)
    {
        await Provider.DeleteAsync(id);
    }

    #endregion
}
