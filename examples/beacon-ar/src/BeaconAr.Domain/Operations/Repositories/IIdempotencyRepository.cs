using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Operations.Repositories;

public interface IIdempotencyRepository : IRepository
{
    Task<IdempotencyRequest?> FindForUpdateAsync(int userId, string operation, byte[] keyHash, CancellationToken cancellationToken);

    void Add(IdempotencyRequest request);
}
