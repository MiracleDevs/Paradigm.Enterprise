using BeaconAr.Domain.Operations;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.Operations;

public interface ICreationIdempotencyProvider : IProvider
{
    Task<CreationResult<T>> ExecuteAsync<T>(
        IdempotencyDescriptor? descriptor,
        string resourceType,
        Func<Task<T>> create,
        Func<int, Task<T>> reload,
        Func<T, int> getId,
        CancellationToken cancellationToken)
        where T : class;
}
