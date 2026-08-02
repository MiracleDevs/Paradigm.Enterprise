using BeaconAr.Domain.Access.Contracts;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.Access;

public interface ICurrentUserProvider : IProvider
{
    Task<CurrentUserDto> ResolveAsync(AuthenticatedIdentity identity, CancellationToken cancellationToken);
}
