using BeaconAr.Interfaces.Operations.Entities;
using IdempotencyStateContract = BeaconAr.Interfaces.Operations.Enums.IdempotencyState;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.Operations.Entities;

public partial class IdempotencyRequest
{
    #region Nested Types

    private sealed record ProposedState(
        int UserId,
        string Operation,
        byte[] KeyHash,
        byte[] RequestHash,
        int StateId,
        string? ResourceType,
        string? ResourceId,
        short? ResponseStatusCode,
        int? CreatedByUserId,
        DateTimeOffset CreationDate,
        int? ModifiedByUserId,
        DateTimeOffset? ModificationDate,
        DateTimeOffset? CompletionDate,
        DateTimeOffset ExpirationDate);

    #endregion

    #region Public Methods

    public static IdempotencyRequest Create(int userId, Operations.IdempotencyDescriptor descriptor, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var value = new IdempotencyRequest
        {
            UserId = userId,
            Operation = descriptor.Operation?.Trim() ?? string.Empty,
            KeyHash = descriptor.KeyHash?.ToArray() ?? [],
            RequestHash = descriptor.RequestHash?.ToArray() ?? [],
            StateId = (int)IdempotencyStateContract.InProgress,
            CreatedByUserId = userId,
            CreationDate = now,
            ExpirationDate = descriptor.ExpirationDate,
        };
        value.Validate();
        return value;
    }

    public void Complete(string resourceType, int resourceId, int userId, DateTimeOffset now)
    {
        EnsureInProgress("completed");
        string normalizedResourceType = resourceType?.Trim() ?? string.Empty;
        string normalizedResourceId = resourceId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var terminal = Normalize(UserId, Operation, KeyHash, RequestHash, (int)IdempotencyStateContract.Completed,
            normalizedResourceType, normalizedResourceId, 201, CreatedByUserId, CreationDate, userId, now, now,
            ExpirationDate);
        var rules = new DomainValidator();
        rules.Assert(resourceId > 0, "Idempotency resource ID must be positive.");
        rules.ThrowIfAny();
        ValidateState(terminal);
        ApplyTerminalState(terminal);
    }

    public void Fail(short responseStatusCode, int userId, DateTimeOffset now)
    {
        EnsureInProgress("failed");
        var terminal = Normalize(UserId, Operation, KeyHash, RequestHash, (int)IdempotencyStateContract.Failed,
            null, null, responseStatusCode, CreatedByUserId, CreationDate, userId, now, now, ExpirationDate);
        ValidateState(terminal);
        ApplyTerminalState(terminal);
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity() => ValidateState(Normalize(UserId, Operation, KeyHash, RequestHash, StateId,
        ResourceType, ResourceId, ResponseStatusCode, CreatedByUserId, CreationDate, ModifiedByUserId,
        ModificationDate, CompletionDate, ExpirationDate));

    partial void BeforeMap(IIdempotencyRequest model)
    {
        _ = Id;
        ValidateState(Normalize(model.UserId, model.Operation, model.KeyHash, model.RequestHash, model.StateId,
            model.ResourceType, model.ResourceId, model.ResponseStatusCode, model.CreatedByUserId,
            model.CreationDate, model.ModifiedByUserId, model.ModificationDate, model.CompletionDate,
            model.ExpirationDate));
    }

    partial void AfterMap(IIdempotencyRequest model)
    {
        ProposedState state = Normalize(UserId, Operation, KeyHash, RequestHash, StateId, ResourceType, ResourceId,
            ResponseStatusCode, CreatedByUserId, CreationDate, ModifiedByUserId, ModificationDate, CompletionDate,
            ExpirationDate);
        Operation = state.Operation;
        KeyHash = state.KeyHash;
        RequestHash = state.RequestHash;
        ResourceType = state.ResourceType;
        ResourceId = state.ResourceId;
    }

    private void ApplyTerminalState(ProposedState state)
    {
        StateId = state.StateId;
        ResourceType = state.ResourceType;
        ResourceId = state.ResourceId;
        ResponseStatusCode = state.ResponseStatusCode;
        ModifiedByUserId = state.ModifiedByUserId;
        ModificationDate = state.ModificationDate;
        CompletionDate = state.CompletionDate;
    }

    private void EnsureInProgress(string transition)
    {
        if (StateId != (int)IdempotencyStateContract.InProgress)
            throw new DomainException($"Only an in-progress idempotency request can be {transition}.");
    }

    private static ProposedState Normalize(int userId, string? operation, byte[]? keyHash, byte[]? requestHash,
        int stateId, string? resourceType, string? resourceId, short? responseStatusCode, int? createdByUserId,
        DateTimeOffset creationDate, int? modifiedByUserId, DateTimeOffset? modificationDate,
        DateTimeOffset? completionDate, DateTimeOffset expirationDate) => new(
        userId,
        operation?.Trim() ?? string.Empty,
        keyHash?.ToArray() ?? [],
        requestHash?.ToArray() ?? [],
        stateId,
        Optional(resourceType),
        Optional(resourceId),
        responseStatusCode,
        createdByUserId,
        creationDate,
        modifiedByUserId,
        modificationDate,
        completionDate,
        expirationDate);

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateState(ProposedState state)
    {
        var rules = new DomainValidator();
        rules.Assert(state.UserId > 0, "Idempotency user ID must be positive.");
        rules.Assert(state.Operation.Length is >= 1 and <= 150, "Idempotency operation is required and cannot exceed 150 characters.");
        rules.Assert(state.KeyHash.Length == 32, "Idempotency key hash must contain exactly 32 bytes.");
        rules.Assert(state.RequestHash.Length == 32, "Idempotency request hash must contain exactly 32 bytes.");
        rules.Assert(Enum.IsDefined((IdempotencyStateContract)state.StateId), "Idempotency state is invalid.");
        rules.Assert(state.CreatedByUserId == state.UserId, "Idempotency creator must match its owning user.");
        rules.Assert(state.CreationDate != default, "Idempotency creation time is required.");
        rules.Assert(state.ExpirationDate > state.CreationDate, "Idempotency expiration must follow creation.");
        rules.Assert(state.ResponseStatusCode is null or >= 100 and <= 599, "Idempotency response status must be between 100 and 599.");
        rules.Assert(state.ResourceType is null || state.ResourceType.Length <= 100, "Idempotency resource type cannot exceed 100 characters.");
        rules.Assert(state.ResourceId is null || state.ResourceId.Length <= 50, "Idempotency resource ID cannot exceed 50 characters.");
        rules.Assert(state.ModifiedByUserId.HasValue == state.ModificationDate.HasValue,
            "Idempotency modification actor and time must be set together.");
        rules.Assert(state.ModifiedByUserId is null or > 0, "Idempotency modification actor must be positive.");
        rules.Assert(state.ModificationDate is null || state.ModificationDate >= state.CreationDate,
            "Idempotency modification cannot precede creation.");
        rules.Assert(state.CompletionDate is null || state.CompletionDate >= state.CreationDate,
            "Idempotency completion cannot precede creation.");

        if (state.StateId == (int)IdempotencyStateContract.InProgress)
        {
            rules.Assert(state.ResourceType is null && state.ResourceId is null && state.ResponseStatusCode is null &&
                         state.CompletionDate is null && state.ModifiedByUserId is null,
                "An in-progress idempotency request cannot contain completion data.");
        }
        else if (state.StateId == (int)IdempotencyStateContract.Completed)
        {
            rules.Assert(state.ResourceType is not null && state.ResourceId is not null &&
                         state.ResponseStatusCode is >= 200 and <= 299 && state.CompletionDate.HasValue &&
                         state.ModifiedByUserId.HasValue && state.ModificationDate == state.CompletionDate,
                "A completed idempotency request requires resource and response data.");
        }
        else if (state.StateId == (int)IdempotencyStateContract.Failed)
        {
            rules.Assert(state.ResourceType is null && state.ResourceId is null &&
                         state.ResponseStatusCode is >= 400 and <= 599 && state.CompletionDate.HasValue &&
                         state.ModifiedByUserId.HasValue && state.ModificationDate == state.CompletionDate,
                "A failed idempotency request requires a failure response and terminal audit data without a resource.");
        }

        rules.ThrowIfAny();
    }

    #endregion
}
