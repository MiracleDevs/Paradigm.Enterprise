using BeaconAr.Domain.Access.Contracts;

namespace BeaconAr.WebApi.Access;

public sealed class CurrentUserAccessor
{
    #region Properties

    public CurrentUserDto? User { get; set; }

    #endregion
}
