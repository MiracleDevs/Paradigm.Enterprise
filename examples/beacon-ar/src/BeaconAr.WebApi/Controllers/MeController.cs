using BeaconAr.Domain.Access.Contracts;
using BeaconAr.WebApi.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaconAr.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController : ControllerBase
{
    #region Fields

    private readonly CurrentUserAccessor _currentUser;

    #endregion

    #region Constructors

    public MeController(CurrentUserAccessor currentUser)
    {
        _currentUser = currentUser;
    }

    #endregion

    #region Public Methods

    [HttpGet(Name = "getCurrentUser")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserDto> Get() => Ok(_currentUser.User ?? throw new InvalidOperationException("Current user is unavailable."));

    #endregion
}
