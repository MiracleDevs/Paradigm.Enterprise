using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.Receivables.Generated;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Access.Repositories;

public interface IApplicationUserRepository : IRepository
{
    Task<ApplicationUser?> FindAsync(string issuer, string subject, CancellationToken cancellationToken);

    Task<ApplicationUser> GetOrCreateAsync(AuthenticatedIdentity identity, DateTimeOffset now, CancellationToken cancellationToken);
}
