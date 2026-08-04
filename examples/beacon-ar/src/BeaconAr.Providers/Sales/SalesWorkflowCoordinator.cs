using System.Security.Cryptography;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Domain.Operations.Entities;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Validation;
using Paradigm.Enterprise.Domain.Uow;

namespace BeaconAr.Providers.Sales;

public sealed class SalesWorkflowCoordinator
{
    #region Fields

    private readonly IAuditLogRepository _auditLogs;

    #endregion

    #region Properties

    internal ISalesPersistenceErrorClassifier ErrorClassifier { get; }

    internal IPersistenceSession PersistenceSession { get; }

    internal IUnitOfWork UnitOfWork { get; }

    internal int UserId => OperationContext.UserId;

    internal DateTimeOffset UtcNow => TimeProvider.GetUtcNow();

    private IApplicationOperationContext OperationContext { get; }

    private TimeProvider TimeProvider { get; }

    #endregion

    #region Constructors

    public SalesWorkflowCoordinator(
        IUnitOfWork unitOfWork,
        IAuditLogRepository auditLogs,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        ISalesPersistenceErrorClassifier errorClassifier,
        IPersistenceSession persistenceSession)
    {
        UnitOfWork = unitOfWork;
        _auditLogs = auditLogs;
        OperationContext = operationContext;
        TimeProvider = timeProvider;
        ErrorClassifier = errorClassifier;
        PersistenceSession = persistenceSession;
    }

    #endregion

    #region Private Methods

    internal async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
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
            PersistenceSession.DiscardTrackedChanges();
            throw Translate(exception);
        }
    }

    internal static void EnsureVersion(byte[] currentVersion, string expectedVersion)
    {
        byte[] expected = SalesRequestValidator.DecodeVersion(expectedVersion);
        if (!CryptographicOperations.FixedTimeEquals(currentVersion, expected))
            throw new SalesException("concurrency_conflict", "The record was changed by another user.");
    }

    internal void AddAudit(
        string resourceType,
        int resourceId,
        string action,
        DateTimeOffset now,
        string? previousStatus = null,
        string? newStatus = null,
        string? metadataJson = null)
    {
        _auditLogs.Add(new AuditLog
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

    internal static SalesException NotFound(string resource) => new("not_found", $"The {resource} was not found.");

    internal static SalesValidationException InvalidReference(string field, string message) =>
        new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [field] = [message] });

    private Exception Translate(Exception exception)
    {
        if (exception is OperationCanceledException or SalesException)
            return exception;
        PersistenceConflictKind conflict = ErrorClassifier.Classify(exception);
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
