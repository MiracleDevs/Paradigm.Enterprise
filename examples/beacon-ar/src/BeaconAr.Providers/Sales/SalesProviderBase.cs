using System.Security.Cryptography;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Repositories;
using BeaconAr.Domain.Sales.Validation;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.Sales;

public abstract class SalesProviderBase : IProvider
{
    #region Fields

    private readonly ISalesPersistenceErrorClassifier _errorClassifier;
    private readonly IPersistenceSession _persistenceSession;

    #endregion

    #region Properties

    protected IUnitOfWork UnitOfWork { get; }
    protected IAuditLogRepository AuditLogs { get; }
    protected IApplicationOperationContext OperationContext { get; }
    protected TimeProvider TimeProvider { get; }
    protected ISalesPersistenceErrorClassifier ErrorClassifier => _errorClassifier;
    protected IPersistenceSession PersistenceSession => _persistenceSession;

    #endregion

    #region Constructors

    protected SalesProviderBase(
        IUnitOfWork unitOfWork,
        IAuditLogRepository auditLogs,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        ISalesPersistenceErrorClassifier errorClassifier,
        IPersistenceSession persistenceSession)
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

    protected async Task<T> ExecuteMutationAsync<T>(Func<Task<T>> operation)
    {
        if (UnitOfWork.HasActiveTransaction)
        {
            try
            {
                return await operation();
            }
            catch (Exception exception)
            {
                throw Translate(exception);
            }
        }

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
            throw Translate(exception);
        }
    }

    protected static void EnsureVersion(byte[] currentVersion, string expectedVersion)
    {
        byte[] expected = SalesRequestValidator.DecodeVersion(expectedVersion);
        if (!CryptographicOperations.FixedTimeEquals(currentVersion, expected))
            throw new SalesException("concurrency_conflict", "The record was changed by another user.");
    }

    protected void AddAudit(
        string resourceType,
        int resourceId,
        string action,
        DateTimeOffset now,
        string? previousStatus = null,
        string? newStatus = null,
        string? metadataJson = null)
    {
        AuditLogs.Add(new AuditLog
        {
            ResourceType = resourceType,
            ResourceId = resourceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Action = action,
            UserId = OperationContext.UserId,
            RecordedAt = now,
            CorrelationId = OperationContext.CorrelationId,
            PreviousStatusCode = previousStatus,
            NewStatusCode = newStatus,
            MetadataJson = metadataJson,
        });
    }

    protected static SalesException NotFound(string resource) => new("not_found", $"The {resource} was not found.");

    protected static SalesValidationException InvalidReference(string field, string message) =>
        new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [field] = [message] });

    #endregion

    #region Private Methods

    private Exception Translate(Exception exception)
    {
        if (exception is OperationCanceledException or SalesException)
            return exception;
        PersistenceConflictKind conflict = _errorClassifier.Classify(exception);
        return conflict switch
        {
            PersistenceConflictKind.None => exception,
            PersistenceConflictKind.Concurrency => new SalesException("concurrency_conflict", "The record was changed by another user.", exception),
            PersistenceConflictKind.Duplicate => new SalesException("duplicate_key", "A conflicting sales record already exists.", exception),
            PersistenceConflictKind.Referenced => new SalesException("reference_conflict", "A referenced record changed while the operation was being processed.", exception),
            _ => new InvalidOperationException("Unsupported persistence conflict classification.", exception),
        };
    }

    #endregion
}
