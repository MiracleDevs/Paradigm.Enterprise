using System.Security.Cryptography;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Domain.Uow;

namespace BeaconAr.Providers.Operations;

public sealed class CreationIdempotencyProvider : ICreationIdempotencyProvider
{
    #region Fields

    private readonly IIdempotencyRepository _requests;
    private readonly IIdempotencyPersistenceErrorClassifier _persistenceErrors;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationOperationContext _operationContext;
    private readonly IPersistenceSession _persistenceSession;
    private readonly TimeProvider _timeProvider;

    #endregion

    #region Constructors

    public CreationIdempotencyProvider(
        IIdempotencyRepository requests,
        IIdempotencyPersistenceErrorClassifier persistenceErrors,
        IUnitOfWork unitOfWork,
        IApplicationOperationContext operationContext,
        IPersistenceSession persistenceSession,
        TimeProvider timeProvider)
    {
        _requests = requests;
        _persistenceErrors = persistenceErrors;
        _unitOfWork = unitOfWork;
        _operationContext = operationContext;
        _persistenceSession = persistenceSession;
        _timeProvider = timeProvider;
    }

    #endregion

    #region Public Methods

    public async Task<CreationResult<T>> ExecuteAsync<T>(
        IdempotencyDescriptor? descriptor,
        string resourceType,
        Func<Task<T>> create,
        Func<int, Task<T>> reload,
        Func<T, int> getId,
        CancellationToken cancellationToken)
        where T : class
    {
        if (descriptor is null)
            return new CreationResult<T>(await create(), true);

        using ITransaction transaction = _unitOfWork.CreateTransaction();
        try
        {
            IdempotencyRequest? existing = await _requests.FindForUpdateAsync(
                _operationContext.UserId, descriptor.Operation, descriptor.KeyHash, cancellationToken);
            if (existing is not null)
            {
                CreationResult<T> replay = await ReplayAsync(existing, descriptor, resourceType, reload);
                transaction.Commit();
                return replay;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            IdempotencyRequest request = IdempotencyRequest.Create(_operationContext.UserId, descriptor, now);
            _requests.Add(request);
            await _unitOfWork.CommitChangesAsync();
            T value = await create();
            request.Complete(resourceType, getId(value), _operationContext.UserId, _timeProvider.GetUtcNow());
            await _unitOfWork.CommitChangesAsync();
            transaction.Commit();
            return new CreationResult<T>(value, true);
        }
        catch (Exception exception)
        {
            if (transaction.IsActive)
                transaction.Rollback();
            _persistenceSession.DiscardTrackedChanges();
            if (!_persistenceErrors.IsKeyConflict(exception))
                throw;

            IdempotencyRequest? winner = await _requests.FindForUpdateAsync(
                _operationContext.UserId, descriptor.Operation, descriptor.KeyHash, cancellationToken);
            if (winner is null)
                throw;
            return await ReplayAsync(winner, descriptor, resourceType, reload);
        }
    }

    #endregion

    #region Private Methods

    private static async Task<CreationResult<T>> ReplayAsync<T>(
        IdempotencyRequest existing,
        IdempotencyDescriptor descriptor,
        string resourceType,
        Func<int, Task<T>> reload)
        where T : class
    {
        if (!CryptographicOperations.FixedTimeEquals(existing.RequestHash, descriptor.RequestHash))
            throw new MasterDataException("idempotency_key_reused", "The idempotency key was already used with a different request.");
        if (existing.StateId != (int)Domain.Operations.IdempotencyState.Completed || existing.ResourceType != resourceType ||
            !int.TryParse(existing.ResourceId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int resourceId))
        {
            throw new MasterDataException("idempotency_in_progress", "An idempotent request with this key is still in progress.");
        }
        return new CreationResult<T>(await reload(resourceId), false);
    }

    #endregion
}
