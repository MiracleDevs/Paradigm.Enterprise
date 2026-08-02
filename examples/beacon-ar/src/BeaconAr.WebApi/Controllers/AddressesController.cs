using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Providers.MasterData;
using BeaconAr.WebApi.Http;
using BeaconAr.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Controllers;

[ApiController]
[Authorize(Policy = BeaconPolicies.Read)]
[Route("api/v1/addresses")]
public sealed class AddressesController : ControllerBase
{
    #region Fields

    private readonly IAddressProvider _provider;
    private readonly CreationIdempotencyService _idempotency;

    #endregion

    #region Constructors

    public AddressesController(IAddressProvider provider, CreationIdempotencyService idempotency)
    {
        _provider = provider;
        _idempotency = idempotency;
    }

    #endregion

    #region Public Methods

    [HttpGet(Name = "searchAddresses")]
    public async Task<ActionResult<PageResult<AddressDto>>> Search(
        [FromQuery(Name = "search")] string? search,
        [FromQuery(Name = "pageNumber")] int pageNumber = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 10,
        [FromQuery(Name = "sortField")] string? sortField = null,
        [FromQuery(Name = "sortDirection")] SortDirection sortDirection = SortDirection.Asc,
        [FromQuery(Name = "customerId")] int? customerId = null,
        [FromQuery(Name = "type")] string? type = null,
        [FromQuery(Name = "usage")] AddressUsage? usage = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _provider.SearchAsync(new AddressSearchRequest
        {
            Search = search, PageNumber = pageNumber, PageSize = pageSize, SortField = sortField,
            SortDirection = sortDirection, CustomerId = customerId, Type = type, Usage = usage,
        }, cancellationToken));

    [HttpGet("{id:int}", Name = "getAddress")]
    public async Task<ActionResult<AddressDto>> Get(int id, CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        AddressDto value = await _provider.GetByIdAsync(id, cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        return Ok(value);
    }

    [HttpPost(Name = "createAddress")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    [ProducesResponseType<AddressDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AddressDto>> Create([FromBody] AddressCreateRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken cancellationToken)
    {
        (AddressDto value, bool replayed) = await _idempotency.ExecuteAsync("create-address", key, request,
            () => _provider.CreateAsync(request, cancellationToken), id => _provider.GetByIdAsync(id, cancellationToken), static value => value.Id, cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        if (replayed) Response.Headers["Idempotency-Replayed"] = "true";
        return CreatedAtRoute("getAddress", new { id = value.Id }, value);
    }

    [HttpPut("{id:int}", Name = "updateAddress")]
    [Authorize(Policy = BeaconPolicies.Write)]
    [Consumes(ApiContract.Json)]
    public async Task<ActionResult<AddressDto>> Update(
        int id,
        [FromBody] AddressUpdateRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ApiContract.EnsurePositiveId(id);
        AddressDto value = await _provider.UpdateAsync(id, request, ETagCodec.ParseRequired(ifMatch), cancellationToken);
        ApiContract.SetETag(Response, value.Version);
        return Ok(value);
    }

    [HttpDelete("{id:int}", Name = "deleteAddress")]
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
