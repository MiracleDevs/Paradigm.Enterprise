using BeaconAr.Domain.Reporting.Contracts;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.Reporting;

public interface IDashboardProvider : IProvider
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
