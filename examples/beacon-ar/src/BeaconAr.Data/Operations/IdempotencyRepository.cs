using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Operations;

public sealed class IdempotencyRepository : RepositoryBase<ReceivablesDbContext, int>, IIdempotencyRepository
{
    #region Constructors

    public IdempotencyRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<IdempotencyRequest?> FindForUpdateAsync(int userId, string operation, byte[] keyHash, CancellationToken cancellationToken) =>
        EntityContext.IdempotencyRequests
            .FromSqlInterpolated($"SELECT * FROM [dbo].[IdempotencyRequest] WITH (UPDLOCK, HOLDLOCK) WHERE [UserId] = {userId} AND [Operation] = {operation} AND [KeyHash] = {keyHash}")
            .SingleOrDefaultAsync(cancellationToken);

    public void Add(IdempotencyRequest request) => EntityContext.IdempotencyRequests.Add(request);

    #endregion
}
