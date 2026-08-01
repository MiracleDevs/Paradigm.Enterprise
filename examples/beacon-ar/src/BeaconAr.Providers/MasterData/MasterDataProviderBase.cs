using System.Security.Cryptography;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Generated;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.MasterData;

public abstract class MasterDataProviderBase : IProvider
{
    #region Fields

    private readonly IMasterDataPersistenceErrorClassifier _errorClassifier;
    private readonly IMasterDataPersistenceSession _persistenceSession;

    #endregion

    #region Properties

    protected IUnitOfWork UnitOfWork { get; }

    protected IAuditLogRepository AuditLogs { get; }

    protected IApplicationOperationContext OperationContext { get; }

    protected TimeProvider TimeProvider { get; }

    #endregion

    #region Constructors

    protected MasterDataProviderBase(
        IUnitOfWork unitOfWork,
        IAuditLogRepository auditLogs,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        IMasterDataPersistenceErrorClassifier errorClassifier,
        IMasterDataPersistenceSession persistenceSession)
    {
        UnitOfWork = unitOfWork;
        AuditLogs = auditLogs;
        OperationContext = operationContext;
        TimeProvider = timeProvider;
        _errorClassifier = errorClassifier;
        _persistenceSession = persistenceSession;
    }

    #endregion

    #region Protected Methods

    protected async Task<T> ExecuteMutationAsync<T>(Func<Task<T>> operation, string referenceCode)
    {
        using ITransaction transaction = UnitOfWork.CreateTransaction();
        try
        {
            T result = await operation();
            transaction.Commit();
            return result;
        }
        catch (Exception exception)
        {
            if (transaction.IsActive)
                transaction.Rollback();

            _persistenceSession.DiscardTrackedChanges();

            if (exception is OperationCanceledException or MasterDataException)
                throw;

            PersistenceConflictKind conflict = _errorClassifier.Classify(exception);
            if (conflict == PersistenceConflictKind.None)
                throw;

            throw conflict switch
            {
                PersistenceConflictKind.Duplicate => new MasterDataException("duplicate_key", "A record with the same unique value already exists.", exception),
                PersistenceConflictKind.Referenced => new MasterDataException(referenceCode, "The record is referenced and cannot be deleted.", exception),
                PersistenceConflictKind.Concurrency => new MasterDataException("concurrency_conflict", "The record was changed by another user.", exception),
                _ => throw new InvalidOperationException("Unsupported persistence conflict classification.", exception),
            };
        }
    }

    protected static void EnsureVersion(byte[] currentVersion, string expectedVersion)
    {
        byte[] expected = VersionTokenCodec.Decode(expectedVersion);
        if (!CryptographicOperations.FixedTimeEquals(currentVersion, expected))
            throw new MasterDataException("concurrency_conflict", "The record was changed by another user.");
    }

    protected void AddAudit(string resourceType, int resourceId, string action, string? metadataJson = null,
        DateTimeOffset? recordedAt = null)
    {
        AuditLogs.Add(new AuditLog
        {
            ResourceType = resourceType,
            ResourceId = resourceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Action = action,
            UserId = OperationContext.UserId,
            RecordedAt = recordedAt ?? TimeProvider.GetUtcNow(),
            CorrelationId = OperationContext.CorrelationId,
            MetadataJson = metadataJson,
        });
    }

    protected static MasterDataException NotFound(string resourceName) =>
        new("not_found", $"The {resourceName} was not found.");

    protected static MasterDataException Duplicate(string fieldName) =>
        new("duplicate_key", $"A record with the same {fieldName} already exists.");

    protected static string GetUpdateAction(bool wasActive, bool isActive) =>
        wasActive == isActive ? "updated" : isActive ? "activated" : "deactivated";

    #endregion
}
