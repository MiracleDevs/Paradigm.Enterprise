using BeaconAr.Data.Access.Context;
using BeaconAr.Data.Access.StoredProcedures;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.Access.Repositories;
using BeaconAr.Domain.Access.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Access.Repositories;

public sealed class ApplicationUserRepository : RepositoryBase<AccessDbContext, int>, IApplicationUserRepository
{
    #region Fields

    private static readonly GetOrCreateApplicationUserProcedure GetOrCreateProcedure = new();

    #endregion

    #region Constructors

    public ApplicationUserRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<ApplicationUser?> FindAsync(string issuer, string subject, CancellationToken cancellationToken) =>
        EntityContext.ApplicationUsers.SingleOrDefaultAsync(
            user => user.Issuer == issuer && user.Subject == subject,
            cancellationToken);

    public async Task<ApplicationUser> GetOrCreateAsync(
        AuthenticatedIdentity identity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int id = await GetOrCreateProcedure.ExecuteAsync(GetDbConnection(), new GetOrCreateApplicationUserParameters
        {
            Issuer = identity.Issuer,
            Subject = identity.Subject,
            DisplayName = identity.DisplayName,
            Email = identity.Email,
            Now = now,
        }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return await EntityContext.ApplicationUsers.SingleAsync(user => user.Id == id, cancellationToken);
    }

    #endregion
}
