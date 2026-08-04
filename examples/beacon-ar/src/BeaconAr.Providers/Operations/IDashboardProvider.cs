using BeaconAr.Domain.Operations.Contracts;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.Operations;

public interface IDashboardProvider : IProvider
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
