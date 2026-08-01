using BeaconAr.Domain.Reporting.Contracts;
using BeaconAr.Domain.Reporting.Repositories;

namespace BeaconAr.Providers.Reporting;

public sealed class DashboardProvider : IDashboardProvider
{
    #region Fields

    private readonly IDashboardRepository _dashboard;

    #endregion

    #region Constructors

    public DashboardProvider(IDashboardRepository dashboard)
    {
        _dashboard = dashboard;
    }

    #endregion

    #region Public Methods

    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken) =>
        _dashboard.GetSummaryAsync(cancellationToken);

    #endregion
}
