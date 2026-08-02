using BeaconAr.Domain.Access.Application;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.Access.Repositories;
using BeaconAr.Domain.Receivables.Generated;
using Paradigm.Enterprise.Domain.Uow;

namespace BeaconAr.Providers.Access;

public sealed class CurrentUserProvider : ICurrentUserProvider
{
    #region Fields

    private readonly IApplicationUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    #endregion

    #region Constructors

    public CurrentUserProvider(
        IApplicationUserRepository users,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    #endregion

    #region Public Methods

    public async Task<CurrentUserDto> ResolveAsync(AuthenticatedIdentity identity, CancellationToken cancellationToken)
    {
        Validate(identity);
        ApplicationUser? user = await _users.FindAsync(identity.Issuer, identity.Subject, cancellationToken);
        if (user is null)
            user = await _users.GetOrCreateAsync(identity, _timeProvider.GetUtcNow(), cancellationToken);

        if (!user.IsActive)
            throw new AccessException("forbidden", "The authenticated user is not permitted to access Beacon AR.");

        if (user.Synchronize(identity, _timeProvider.GetUtcNow()))
            await _unitOfWork.CommitChangesAsync();

        return new CurrentUserDto(user.Id, user.DisplayName, user.Email, identity.Policies);
    }

    #endregion

    #region Private Methods

    private static void Validate(AuthenticatedIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(identity.Issuer) || identity.Issuer.Length > 400 ||
            string.IsNullOrWhiteSpace(identity.Subject) || identity.Subject.Length > 200 ||
            string.IsNullOrWhiteSpace(identity.DisplayName) || identity.DisplayName.Length > 200 ||
            identity.Email?.Length > 320)
        {
            throw new AccessException("forbidden", "The authenticated identity is incomplete.");
        }
    }

    #endregion
}
