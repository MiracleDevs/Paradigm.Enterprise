using BeaconAr.Data.Mappers;
using BeaconAr.Data.Receivables;
using BeaconAr.Data.Reporting.StoredProcedures;
using BeaconAr.Domain.Reporting.Contracts;
using BeaconAr.Domain.Reporting.Repositories;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Reporting;

public sealed class DashboardRepository : RepositoryBase<ReceivablesDbContext, int>, IDashboardRepository
{
    #region Fields

    private static readonly GetDashboardSummaryProcedure Procedure = new();

    #endregion

    #region Constructors

    public DashboardRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Static Constructors

    static DashboardRepository() => StoreProcedureMappersRegisterer.RegisterMappers();

    #endregion

    #region Public Methods

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DashboardRow row = await Procedure.ExecuteAsync(GetDbConnection(), new DashboardParameters(), UnitOfWork)
            ?? throw new InvalidOperationException("The dashboard routine returned no summary row.");
        cancellationToken.ThrowIfCancellationRequested();
        return new DashboardSummaryDto(row.Products, row.Customers, row.Carriers, row.OpenQuotes, row.ActiveOrders, row.AsOf);
    }

    #endregion
}
