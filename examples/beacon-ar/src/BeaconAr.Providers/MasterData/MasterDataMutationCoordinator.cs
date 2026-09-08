using System.Security.Cryptography;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using BeaconAr.Domain.Operations.Entities;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.Operations.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class MasterDataMutationCoordinator
{
    #region Fields

    private readonly IAuditLogRepository _auditLogs;
    private readonly IMasterDataPersistenceErrorClassifier _errorClassifier;
    private readonly IApplicationOperationContext _operationContext;
    private readonly IPersistenceSession _persistenceSession;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    #endregion

    #region Properties

    public int UserId => _operationContext.UserId;

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    #endregion

    #region Constructors

    public MasterDataMutationCoordinator(
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        IMasterDataPersistenceErrorClassifier errorClassifier,
        IPersistenceSession persistenceSession)
    {
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
        _operationContext = operationContext;
        _timeProvider = timeProvider;
        _errorClassifier = errorClassifier;
        _persistenceSession = persistenceSession;
    }

    #endregion

    #region Public Methods

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string referenceCode)
    {
        if (_unitOfWork.HasActiveTransaction)
        {
            try
            {
                return await operation();
            }
            catch (Exception exception)
            {
                throw Translate(exception, referenceCode);
            }
        }

        using ITransaction transaction = _unitOfWork.CreateTransaction();
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
            throw Translate(exception, referenceCode);
        }
    }

    public static void EnsureVersion(byte[] currentVersion, string expectedVersion)
    {
        byte[] expected = VersionTokenCodec.Decode(expectedVersion);
        if (!CryptographicOperations.FixedTimeEquals(currentVersion, expected))
            throw new MasterDataException("concurrency_conflict", "The record was changed by another user.");
    }

    public void AddAudit(string resourceType, int resourceId, string action, string? metadataJson = null,
        DateTimeOffset? recordedAt = null)
    {
        _auditLogs.Add(AuditLog.Create(resourceType,
            resourceId.ToString(System.Globalization.CultureInfo.InvariantCulture), action,
            _operationContext.UserId, recordedAt ?? _timeProvider.GetUtcNow(),
            _operationContext.CorrelationId, metadataJson: metadataJson));
    }

    public static MasterDataException NotFound(string resourceName) =>
        new("not_found", $"The {resourceName} was not found.");

    public static MasterDataException Duplicate(string fieldName) =>
        new("duplicate_key", $"A record with the same {fieldName} already exists.");

    public static string GetUpdateAction(bool wasActive, bool isActive) =>
        wasActive == isActive ? "updated" : isActive ? "activated" : "deactivated";

    #endregion

    #region Private Methods

    private Exception Translate(Exception exception, string referenceCode)
    {
        if (exception is OperationCanceledException or MasterDataException)
            return exception;

        PersistenceConflictKind conflict = _errorClassifier.Classify(exception);
        return conflict switch
        {
            PersistenceConflictKind.None => exception,
            PersistenceConflictKind.Duplicate => new MasterDataException("duplicate_key", "A record with the same unique value already exists.", exception),
            PersistenceConflictKind.Referenced => new MasterDataException(referenceCode, "The record is referenced and cannot be deleted.", exception),
            PersistenceConflictKind.Concurrency => new MasterDataException("concurrency_conflict", "The record was changed by another user.", exception),
            _ => new InvalidOperationException("Unsupported persistence conflict classification.", exception),
        };
    }

    #endregion
}
