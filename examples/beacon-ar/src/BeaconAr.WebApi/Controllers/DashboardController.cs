using BeaconAr.Domain.Reporting.Contracts;
using BeaconAr.Providers.Reporting;
using BeaconAr.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Controllers;

[ApiController]
[Authorize(Policy = BeaconPolicies.Read)]
[Route("api/v1/dashboard")]
public sealed class DashboardController : ControllerBase
{
    #region Fields

    private readonly IDashboardProvider _provider;

    #endregion

    #region Constructors

    public DashboardController(IDashboardProvider provider)
    {
        _provider = provider;
    }

    #endregion

    #region Public Methods

    [HttpGet("summary", Name = "getDashboardSummary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await _provider.GetSummaryAsync(cancellationToken));

    #endregion
}
