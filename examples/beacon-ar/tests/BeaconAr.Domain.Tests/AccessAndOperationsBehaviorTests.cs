using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.Access.Entities;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Entities;
using BeaconAr.Domain.Sales.Entities;
using Paradigm.Enterprise.Domain.Exceptions;
using QuoteState = BeaconAr.Interfaces.Sales.Enums.QuoteStatus;
using OrderState = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;
using IdempotencyState = BeaconAr.Interfaces.Operations.Enums.IdempotencyState;

namespace BeaconAr.Domain.Tests;

[TestClass]
public sealed class AccessAndOperationsBehaviorTests
{
    #region Public Methods

    [TestMethod]
    public void ApplicationUserOwnsIdentityNormalizationAndAtomicSynchronization()
    {
        var identity = new AuthenticatedIdentity(" issuer ", " subject ", " Display Name ", " user@example.com ", []);
        ApplicationUser user = ApplicationUser.Create(identity, DateTimeOffset.UnixEpoch);

        Assert.AreEqual("issuer", user.Issuer);
        Assert.AreEqual("subject", user.Subject);
        Assert.AreEqual("Display Name", user.DisplayName);

        var invalid = identity with { DisplayName = "Changed", Email = "not an email" };
        Assert.Throws<DomainException>(() => user.Synchronize(invalid, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.AreEqual("Display Name", user.DisplayName);
        Assert.AreEqual("user@example.com", user.Email);
    }

    [TestMethod]
    public void AuditLogFactoryNormalizesAndValidatesMetadata()
    {
        AuditLog entry = AuditLog.Create(" Quote ", " 42 ", " Created ", 7, DateTimeOffset.UnixEpoch,
            " correlation ", metadataJson: "{\"source\":\"test\"}");

        Assert.AreEqual("Quote", entry.ResourceType);
        Assert.AreEqual("42", entry.ResourceId);
        Assert.AreEqual("Created", entry.Action);
        Assert.Throws<DomainException>(() => AuditLog.Create("Quote", "42", "Created", 7,
            DateTimeOffset.UnixEpoch, "correlation", metadataJson: "not-json"));

        var malformed = new AuditLog
        {
            ResourceType = null!,
            ResourceId = null!,
            Action = null!,
            CorrelationId = null!,
            RecordedAt = DateTimeOffset.UnixEpoch,
            UserId = 7,
        };
        Assert.Throws<DomainException>(malformed.Validate);
    }

    [TestMethod]
    public void IdempotencyRequestOwnsCreationCompletionAndAtomicValidation()
    {
        var descriptor = new IdempotencyDescriptor(" create-quote ", new byte[32], new byte[32],
            DateTimeOffset.UnixEpoch.AddHours(1));
        IdempotencyRequest request = IdempotencyRequest.Create(7, descriptor, DateTimeOffset.UnixEpoch);

        Assert.AreEqual("create-quote", request.Operation);
        Assert.Throws<DomainException>(() => request.Complete("Quote", 42, 0, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.IsNull(request.ResourceType);
        Assert.IsNull(request.CompletionDate);

        request.Complete(" Quote ", 42, 7, DateTimeOffset.UnixEpoch.AddMinutes(1));
        Assert.AreEqual("Quote", request.ResourceType);
        Assert.AreEqual("42", request.ResourceId);
        request.Validate();
    }

    [TestMethod]
    public void IdempotencyValidationIsNullSafeAndCoversEveryCatalogState()
    {
        IdempotencyRequest malformed = Request();
        malformed.Operation = null!;
        malformed.KeyHash = null!;
        malformed.RequestHash = null!;
        Assert.Throws<DomainException>(malformed.Validate);

        IdempotencyRequest inProgress = Request();
        inProgress.Validate();
        Assert.AreEqual((int)IdempotencyState.InProgress, inProgress.StateId);

        IdempotencyRequest completed = Request();
        completed.Complete("Quote", 42, 7, DateTimeOffset.UnixEpoch.AddMinutes(1));
        completed.Validate();
        Assert.AreEqual((int)IdempotencyState.Completed, completed.StateId);

        IdempotencyRequest failed = Request();
        failed.Fail(409, 7, DateTimeOffset.UnixEpoch.AddMinutes(1));
        failed.Validate();
        Assert.AreEqual((int)IdempotencyState.Failed, failed.StateId);
        Assert.AreEqual((short)409, failed.ResponseStatusCode);
        Assert.IsNull(failed.ResourceType);
        Assert.IsNull(failed.ResourceId);
    }

    [TestMethod]
    public void IdempotencyTerminalTransitionsAreAtomicAndRejectInvalidShapes()
    {
        IdempotencyRequest request = Request();
        string original = Snapshot(request);

        Assert.Throws<DomainException>(() => request.Fail(399, 8, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.AreEqual(original, Snapshot(request));
        Assert.Throws<DomainException>(() => request.Fail(500, 8, DateTimeOffset.UnixEpoch.AddMinutes(-1)));
        Assert.AreEqual(original, Snapshot(request));
        Assert.Throws<DomainException>(() => request.Complete("Quote", 0, 8, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.AreEqual(original, Snapshot(request));

        request.Fail(500, 8, DateTimeOffset.UnixEpoch.AddMinutes(1));
        string failed = Snapshot(request);
        Assert.Throws<DomainException>(() => request.Fail(500, 8, DateTimeOffset.UnixEpoch.AddMinutes(2)));
        Assert.Throws<DomainException>(() => request.Complete("Quote", 42, 8, DateTimeOffset.UnixEpoch.AddMinutes(2)));
        Assert.AreEqual(failed, Snapshot(request));

        IdempotencyRequest invalidState = Request();
        invalidState.StateId = 999;
        Assert.Throws<DomainException>(invalidState.Validate);

        IdempotencyRequest invalidFailed = Request();
        invalidFailed.StateId = (int)IdempotencyState.Failed;
        invalidFailed.ResponseStatusCode = 500;
        Assert.Throws<DomainException>(invalidFailed.Validate);
    }

    [TestMethod]
    public void StatusHistoryFactoriesRejectInvalidActors()
    {
        var quote = new Quote { StatusId = (int)QuoteState.Draft };
        var order = new SalesOrder { StatusId = (int)OrderState.Draft };

        Assert.Throws<DomainException>(() => QuoteStatusHistory.Create(quote, 0, DateTimeOffset.UnixEpoch));
        Assert.Throws<DomainException>(() => SalesOrderStatusHistory.Create(order, 0, DateTimeOffset.UnixEpoch));
    }

    #endregion

    #region Private Methods

    private static IdempotencyRequest Request() => IdempotencyRequest.Create(7,
        new IdempotencyDescriptor("create-quote", new byte[32], new byte[32],
            DateTimeOffset.UnixEpoch.AddHours(1)), DateTimeOffset.UnixEpoch);

    private static string Snapshot(IdempotencyRequest request) => string.Join('\u001f',
        request.UserId, request.Operation, Convert.ToBase64String(request.KeyHash ?? []),
        Convert.ToBase64String(request.RequestHash ?? []), request.StateId, request.ResourceType,
        request.ResourceId, request.ResponseStatusCode, request.CreatedByUserId, request.CreationDate,
        request.ModifiedByUserId, request.ModificationDate, request.CompletionDate, request.ExpirationDate);

    #endregion
}
