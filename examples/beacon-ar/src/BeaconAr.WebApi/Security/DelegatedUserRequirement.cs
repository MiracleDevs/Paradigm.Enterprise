using Microsoft.AspNetCore.Authorization;

namespace BeaconAr.WebApi.Security;

internal sealed class DelegatedUserRequirement : IAuthorizationRequirement
{
    #region Properties

    public static DelegatedUserRequirement Instance { get; } = new();

    #endregion

    #region Constructors

    private DelegatedUserRequirement()
    {
    }

    #endregion
}
