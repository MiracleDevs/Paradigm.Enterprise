using BeaconAr.Data.Operations.Context;
using BeaconAr.Data.Operations.StoredProcedures;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Operations.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Operations.Repositories;

public sealed class IdempotencyRepository : RepositoryBase<OperationsDbContext, int>, IIdempotencyRepository
{
    #region Fields

    private static readonly LockIdempotencyRequestProcedure LockProcedure = new();

    #endregion

    #region Constructors

    public IdempotencyRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public async Task<IdempotencyRequest?> FindForUpdateAsync(int userId, string operation, byte[] keyHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        long id = await LockProcedure.ExecuteAsync(GetDbConnection(), new LockIdempotencyRequestParameters
        {
            UserId = userId,
            Operation = operation,
            KeyHash = keyHash,
        }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return id == 0
            ? null
            : await EntityContext.IdempotencyRequests.SingleAsync(request => request.Id == id, cancellationToken);
    }

    public void Add(IdempotencyRequest request) => EntityContext.IdempotencyRequests.Add(request);

    #endregion
}
