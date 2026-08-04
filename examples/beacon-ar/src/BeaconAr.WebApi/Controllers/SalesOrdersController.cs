using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Entities;
using SalesOrderStatus = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;
using BeaconAr.Providers.Sales;
using BeaconAr.WebApi.Http;
using BeaconAr.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Controllers;

[ApiController]
[Authorize(Policy = BeaconPolicies.Read)]
[Route("api/v1/sales-orders")]
public sealed class SalesOrdersController : ControllerBase
{
    #region Fields

    private readonly ISalesOrderProvider _provider;
    private readonly CreationIdempotencyService _idempotency;

    #endregion

    #region Constructors

    public SalesOrdersController(ISalesOrderProvider provider, CreationIdempotencyService idempotency)
    {
        _provider = provider;
        _idempotency = idempotency;
    }

    #endregion

    #region Public Methods

    [HttpGet(Name = "searchSalesOrders")]
    public async Task<ActionResult<PageResult<SalesOrderView>>> Search(
        [FromQuery(Name = "search")] string? search,
        [FromQuery(Name = "status")] SalesOrderStatus? status = null,
        [FromQuery(Name = "customerId")] int? customerId = null,
        [FromQuery(Name = "sourceQuoteId")] int? sourceQuoteId = null,
        [FromQuery(Name = "pageNumber")] int pageNumber = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10,
        [FromQuery(Name = "sortField")] string? sortField = null,
        [FromQuery(Name = "sortDirection")] SortDirection sortDirection = SortDirection.Desc,
        CancellationToken cancellationToken = default) =>
        Ok(await _provider.SearchAsync(new SalesOrderSearchRequest(search, status, customerId, sourceQuoteId, pageNumber, pageSize, sortField, sortDirection), cancellationToken));

    [HttpGet("{id:int}", Name = "getSalesOrder")]
    public async Task<ActionResult<SalesOrderDto>> Get(int id, CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        SalesOrderDto value = await _provider.GetByIdAsync(id, cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        return Ok(value);
    }

    [HttpPost(Name = "createSalesOrder")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SalesOrderDto>> Create([FromBody] SalesOrderCreateRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken cancellationToken)
    {
        (SalesOrderDto value, bool replayed) = await _idempotency.ExecuteAsync("create-sales-order", key, request,
            () => _provider.CreateDirectAsync(request, cancellationToken), id => _provider.GetByIdAsync(id, cancellationToken), static value => value.Id, cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        if (replayed) Response.Headers["Idempotency-Replayed"] = "true";
        return CreatedAtRoute("getSalesOrder", new { id = value.Id }, value);
    }

    [HttpPut("{id:int}", Name = "updateSalesOrder")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<SalesOrderDto>> Update(
        int id,
        [FromBody] SalesOrderUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        SalesOrderDto value = await _provider.UpdateAsync(id, request, ETagCodec.ParseRequired(ifMatch), cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        return Ok(value);
    }

    [HttpDelete("{id:int}", Name = "deleteSalesOrder")]
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

    [HttpPost("{id:int}/status-transitions", Name = "transitionSalesOrderStatus")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    public async Task<ActionResult<SalesOrderDto>> Transition(
        int id,
        [FromBody] SalesOrderStatusTransitionRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        SalesOrderDto value = await _provider.TransitionAsync(id, request, ETagCodec.ParseRequired(ifMatch), cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        return Ok(value);
    }

    #endregion
}
