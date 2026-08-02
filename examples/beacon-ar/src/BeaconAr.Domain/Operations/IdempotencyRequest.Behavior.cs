namespace BeaconAr.Domain.Receivables.Entities;

public partial class IdempotencyRequest
{
    #region Public Methods

    public static IdempotencyRequest Create(int userId, Operations.IdempotencyDescriptor descriptor, DateTimeOffset now) => new()
    {
        UserId = userId,
        Operation = descriptor.Operation,
        KeyHash = descriptor.KeyHash,
        RequestHash = descriptor.RequestHash,
        StateId = (int)Operations.IdempotencyState.InProgress,
        CreatedByUserId = userId,
        CreationDate = now,
        ExpirationDate = descriptor.ExpirationDate,
    };

    public void Complete(string resourceType, int resourceId, int userId, DateTimeOffset now)
    {
        StateId = (int)Operations.IdempotencyState.Completed;
        ResourceType = resourceType;
        ResourceId = resourceId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ResponseStatusCode = 201;
        ModifiedByUserId = userId;
        ModificationDate = now;
        CompletionDate = now;
    }

    #endregion
}
