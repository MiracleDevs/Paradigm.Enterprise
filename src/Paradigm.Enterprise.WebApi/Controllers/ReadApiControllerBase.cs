using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Providers;
using Paradigm.Enterprise.WebApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.WebApi.Controllers;

/// <summary>
/// Exposes conventional paginated search and identifier lookup actions for a read provider.
/// </summary>
/// <typeparam name="TProvider">The read provider that executes queries.</typeparam>
/// <typeparam name="TView">The view model returned to clients.</typeparam>
/// <typeparam name="TParameters">The pagination parameters accepted by search.</typeparam>
/// <typeparam name="TId">The value-type entity identifier.</typeparam>
/// <remarks>
/// Both actions are explicitly exposed, but this base controller also allows anonymous access.
/// Standard <c>[Authorize]</c> metadata on a derived controller does not override the inherited
/// <see cref="AllowAnonymousAttribute"/>. Protected controllers must use an independently enforced
/// mechanism such as <see cref="ApiAuthorizationAttribute"/>, or replace this base controller with
/// one that does not allow anonymous access. Endpoint exposure does not authenticate a caller.
/// </remarks>
/// <example>
/// Apply the framework's independently enforced authorization filter because this base class permits
/// anonymous access:
/// <code>
/// [Route("api/products")]
/// [ApiAuthorization]
/// public sealed class ProductsController
///     : ReadApiControllerBase&lt;IProductProvider, ProductView, ProductSearch, int&gt;
/// {
///     public ProductsController(
///         ILogger&lt;ApiControllerBase&gt; logger,
///         IProductProvider provider)
///         : base(logger, provider)
///     {
///     }
/// }
/// </code>
/// Here <c>IProductProvider</c> implements <c>IReadProvider&lt;ProductView, int&gt;</c>, and
/// <c>ProductSearch</c> derives from <see cref="PaginationParametersBase"/>.
/// </example>
[AllowAnonymous]
[ApiController]
public abstract class ReadApiControllerBase<TProvider, TView, TParameters, TId> : ApiControllerBase<TProvider>
    where TId : struct, IEquatable<TId>
    where TProvider : IReadProvider<TView, TId>
    where TParameters : PaginationParametersBase
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadApiControllerBase{TProvider, TView, TParameters, TId}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The provider.</param>
    protected ReadApiControllerBase(ILogger<ApiControllerBase> logger, TProvider provider)
        : base(logger, provider)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes the search using the specified parameters.
    /// </summary>
    /// <param name="parameters">The validated pagination and filter parameters.</param>
    /// <returns>A page of views and its pagination metadata.</returns>
    [HttpPost("search")]
    [ExposeEndpoint]
    public virtual async Task<PaginatedResultDto<TView>> SearchAsync([FromBody, Required] TParameters parameters)
    {
        return await Provider.SearchAsync(parameters);
    }

    /// <summary>
    /// Gets the entity by identifier.
    /// </summary>
    /// <param name="id">The identifier of the view to retrieve.</param>
    /// <returns>The view returned by the provider.</returns>
    [HttpGet("get-by-id")]
    [ExposeEndpoint]
    public virtual async Task<TView> GetByIdAsync([FromQuery] TId id)
    {
        return await Provider.GetByIdAsync(id);
    }

    #endregion
}
