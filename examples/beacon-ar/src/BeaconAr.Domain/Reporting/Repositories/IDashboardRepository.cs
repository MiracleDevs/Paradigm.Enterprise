using BeaconAr.Domain.Reporting.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Reporting.Repositories;

public interface IDashboardRepository : IRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
