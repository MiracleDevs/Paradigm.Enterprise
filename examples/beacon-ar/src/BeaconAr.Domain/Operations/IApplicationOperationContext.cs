namespace BeaconAr.Domain.Operations;

public interface IApplicationOperationContext
{
    int UserId { get; }

    string CorrelationId { get; }
}
