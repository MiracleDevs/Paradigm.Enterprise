using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using BeaconAr.Providers.MasterData;
using BeaconAr.WebApi.Http;
using BeaconAr.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Controllers;

[ApiController]
[Authorize(Policy = BeaconPolicies.Read)]
[Route("api/v1/customers")]
public sealed class CustomersController : ControllerBase
{
    #region Fields

    private readonly ICustomerProvider _provider;
    private readonly CreationIdempotencyService _idempotency;

    #endregion

    #region Constructors

    public CustomersController(ICustomerProvider provider, CreationIdempotencyService idempotency)
    {
        _provider = provider;
        _idempotency = idempotency;
    }

    #endregion

    #region Public Methods

    [HttpGet(Name = "searchCustomers")]
    public async Task<ActionResult<PageResult<CustomerView>>> Search(
        [FromQuery(Name = "search")] string? search,
        [FromQuery(Name = "pageNumber")] int pageNumber = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10,
        [FromQuery(Name = "sortField")] string? sortField = null,
        [FromQuery(Name = "sortDirection")] SortDirection sortDirection = SortDirection.Asc,
        [FromQuery(Name = "active")] bool? active = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _provider.SearchAsync(new CustomerSearchRequest
        {
            Search = search, PageNumber = pageNumber, PageSize = pageSize, SortField = sortField,
            SortDirection = sortDirection, Active = active,
        }, cancellationToken));

    [HttpGet("{id:int}", Name = "getCustomer")]
    public async Task<ActionResult<CustomerView>> Get(int id, CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        CustomerView value = await _provider.GetByIdAsync(id, cancellationToken);
        ApiContract.SetETag(Response, value.RowVersion);
        return Ok(value);
    }

    [HttpPost(Name = "createCustomer")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    [ProducesResponseType<CustomerView>(StatusCodes.Status201Created)]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<CustomerView>> Create([FromBody] CustomerCreateRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken cancellationToken)
    {
        (CustomerView value, bool replayed) = await _idempotency.ExecuteAsync("create-customer", key, request,
            () => _provider.CreateAsync(request, cancellationToken), id => _provider.GetByIdAsync(id, cancellationToken), static value => value.Id, cancellationToken);
        ApiContract.SetETag(Response, value.RowVersion);
        if (replayed) Response.Headers["Idempotency-Replayed"] = "true";
        return CreatedAtRoute("getCustomer", new { id = value.Id }, value);
    }

    [HttpPut("{id:int}", Name = "updateCustomer")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    public async Task<ActionResult<CustomerView>> Update(
        int id,
        [FromBody] CustomerUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        CustomerView value = await _provider.UpdateAsync(id, request, ETagCodec.ParseRequired(ifMatch), cancellationToken);
        ApiContract.SetETag(Response, value.RowVersion);
        return Ok(value);
    }

    [HttpDelete("{id:int}", Name = "deleteCustomer")]
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
