using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Providers.MasterData;
using BeaconAr.WebApi.Http;
using BeaconAr.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Controllers;

[ApiController]
[Authorize(Policy = BeaconPolicies.Read)]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    #region Fields

    private readonly IProductProvider _provider;
    private readonly CreationIdempotencyService _idempotency;

    #endregion

    #region Constructors

    public ProductsController(IProductProvider provider, CreationIdempotencyService idempotency)
    {
        _provider = provider;
        _idempotency = idempotency;
    }

    #endregion

    #region Public Methods

    [HttpGet(Name = "searchProducts")]
    public async Task<ActionResult<PageResult<ProductView>>> Search(
        [FromQuery(Name = "search")] string? search,
        [FromQuery(Name = "pageNumber")] int pageNumber = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10,
        [FromQuery(Name = "sortField")] string? sortField = null,
        [FromQuery(Name = "sortDirection")] SortDirection sortDirection = SortDirection.Asc,
        [FromQuery(Name = "active")] bool? active = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _provider.SearchAsync(new ProductSearchRequest
        {
            Search = search, PageNumber = pageNumber, PageSize = pageSize, SortField = sortField,
            SortDirection = sortDirection, Active = active,
        }, cancellationToken));

    [HttpGet("{id:int}", Name = "getProduct")]
    public async Task<ActionResult<ProductView>> Get(int id, CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        ProductView value = await _provider.GetByIdAsync(id, cancellationToken);
        ApiContract.SetETag(Response, value.RowVersion);
        return Ok(value);
    }

    [HttpPost(Name = "createProduct")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType<ProductView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductView>> Create(
        [FromBody] ProductCreateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        (ProductView value, bool replayed) = await _idempotency.ExecuteAsync(
            "create-product", idempotencyKey, request, () => _provider.CreateAsync(request, cancellationToken),
            id => _provider.GetByIdAsync(id, cancellationToken), static value => value.Id, cancellationToken);
        ApiContract.SetETag(Response, value.RowVersion);
        if (replayed)
            Response.Headers["Idempotency-Replayed"] = "true";
        return CreatedAtRoute("getProduct", new { id = value.Id }, value);
    }

    [HttpPut("{id:int}", Name = "updateProduct")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<ProductView>> Update(
        int id,
        [FromBody] ProductUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        ProductView value = await _provider.UpdateAsync(id, request, ETagCodec.ParseRequired(ifMatch), cancellationToken);
        ApiContract.SetETag(Response, value.RowVersion);
        return Ok(value);
    }

    [HttpDelete("{id:int}", Name = "deleteProduct")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        int id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        await _provider.DeleteAsync(id, ETagCodec.ParseRequired(ifMatch), cancellationToken);
        return NoContent();
    }

    #endregion
}
