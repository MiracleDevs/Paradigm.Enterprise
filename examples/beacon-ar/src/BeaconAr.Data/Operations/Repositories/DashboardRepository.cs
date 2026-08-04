using BeaconAr.Data.Operations.Context;
using BeaconAr.Data.Operations.StoredProcedures;
using BeaconAr.Domain.Operations.Contracts;
using BeaconAr.Domain.Operations.Repositories;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Operations.Repositories;

public sealed class DashboardRepository : RepositoryBase<OperationsDbContext, int>, IDashboardRepository
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
