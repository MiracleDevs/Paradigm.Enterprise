namespace BeaconAr.Domain.MasterData.Application;

public interface IApplicationOperationContext
{
    int UserId { get; }

    string CorrelationId { get; }
}
