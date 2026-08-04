using BeaconAr.Domain.Operations.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Operations.Repositories;

public interface IDashboardRepository : IRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
