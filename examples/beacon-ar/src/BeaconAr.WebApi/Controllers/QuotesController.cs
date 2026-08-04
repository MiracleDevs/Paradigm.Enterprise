using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Entities;
using QuoteStatus = BeaconAr.Interfaces.Sales.Enums.QuoteStatus;
using BeaconAr.Providers.Sales;
using BeaconAr.WebApi.Http;
using BeaconAr.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Controllers;

[ApiController]
[Authorize(Policy = BeaconPolicies.Read)]
[Route("api/v1/quotes")]
public sealed class QuotesController : ControllerBase
{
    #region Fields

    private readonly IQuoteProvider _provider;
    private readonly IQuoteConversionProvider _conversion;
    private readonly CreationIdempotencyService _idempotency;

    #endregion

    #region Constructors

    public QuotesController(IQuoteProvider provider, IQuoteConversionProvider conversion, CreationIdempotencyService idempotency)
    {
        _provider = provider;
        _conversion = conversion;
        _idempotency = idempotency;
    }

    #endregion

    #region Public Methods

    [HttpGet(Name = "searchQuotes")]
    public async Task<ActionResult<PageResult<QuoteView>>> Search(
        [FromQuery(Name = "search")] string? search,
        [FromQuery(Name = "status")] QuoteStatus? status = null,
        [FromQuery(Name = "customerId")] int? customerId = null,
        [FromQuery(Name = "pageNumber")] int pageNumber = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10,
        [FromQuery(Name = "sortField")] string? sortField = null,
        [FromQuery(Name = "sortDirection")] SortDirection sortDirection = SortDirection.Desc,
        CancellationToken cancellationToken = default) =>
        Ok(await _provider.SearchAsync(new QuoteSearchRequest(search, status, customerId, pageNumber, pageSize, sortField, sortDirection), cancellationToken));

    [HttpGet("{id:int}", Name = "getQuote")]
    public async Task<ActionResult<QuoteDto>> Get(int id, CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        QuoteDto value = await _provider.GetByIdAsync(id, cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        return Ok(value);
    }

    [HttpPost(Name = "createQuote")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<QuoteDto>> Create([FromBody] QuoteCreateRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken cancellationToken)
    {
        (QuoteDto value, bool replayed) = await _idempotency.ExecuteAsync("create-quote", key, request,
            () => _provider.CreateAsync(request, cancellationToken), id => _provider.GetByIdAsync(id, cancellationToken), static value => value.Id, cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        if (replayed) Response.Headers["Idempotency-Replayed"] = "true";
        return CreatedAtRoute("getQuote", new { id = value.Id }, value);
    }

    [HttpPut("{id:int}", Name = "updateQuote")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<QuoteDto>> Update(
        int id,
        [FromBody] QuoteUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        QuoteDto value = await _provider.UpdateAsync(id, request, ETagCodec.ParseRequired(ifMatch), cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        return Ok(value);
    }

    [HttpDelete("{id:int}", Name = "deleteQuote")]
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

    [HttpPost("{id:int}/status-transitions", Name = "transitionQuoteStatus")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    public async Task<ActionResult<QuoteDto>> Transition(
        int id,
        [FromBody] QuoteStatusTransitionRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        QuoteDto value = await _provider.TransitionAsync(id, request, ETagCodec.ParseRequired(ifMatch), cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        return Ok(value);
    }

    [HttpPut("{id:int}/sales-order", Name = "convertQuoteToSalesOrder")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SalesOrderDto>> Convert(int id, CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        QuoteConversionResult result = await _conversion.ConvertAsync(id, cancellationToken);
        ApiContract.SetETag(Response, result.SalesOrder.Version);
        return result.Created
            ? CreatedAtRoute("getSalesOrder", new { id = result.SalesOrder.Id }, result.SalesOrder)
            : Ok(result.SalesOrder);
    }

    #endregion
}
