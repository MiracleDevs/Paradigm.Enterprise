using BeaconAr.Data.Mappers;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData;

public sealed class CarrierViewRepository : RepositoryBase<ReceivablesDbContext, int>, ICarrierViewRepository
{
    #region Fields

    private static readonly SearchCarrierProcedure SearchProcedure = new();

    #endregion

    #region Constructors

    public CarrierViewRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Static Constructors

    static CarrierViewRepository() => StoreProcedureMappersRegisterer.RegisterMappers();

    #endregion

    #region Public Methods

    public async Task<CarrierDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Carrier? carrier = await EntityContext.Carriers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return carrier is null ? null : Map(carrier);
    }

    public async Task<PageResult<CarrierDto>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        string sort = string.IsNullOrWhiteSpace(request.SortField) ? "id" : request.SortField.Trim().ToLowerInvariant();
        cancellationToken.ThrowIfCancellationRequested();
        var (count, rows) = await SearchProcedure.ExecuteAsync(GetDbConnection(), new CarrierSearchParameters
        {
            Search = search,
            Active = request.Active,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortField = sort,
            SortDirection = request.SortDirection == SortDirection.Desc ? "desc" : "asc",
        }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return PageResultFactory.Create((rows ?? []).Select(Map).ToArray(), request.PageNumber, request.PageSize, count);
    }

    #endregion

    #region Private Methods

    private static CarrierDto Map(Carrier carrier) => new(
        carrier.Id, carrier.Code, carrier.Name, carrier.ServiceLevel, carrier.TrackingUrlTemplate, carrier.IsActive,
        carrier.CreatedByUserId, carrier.CreationDate, carrier.ModifiedByUserId, carrier.ModificationDate,
        VersionTokenCodec.Encode(carrier.RowVersion));

    private static CarrierDto Map(CarrierSearchRow carrier) => new(
        carrier.Id, carrier.Code, carrier.Name, carrier.ServiceLevel, carrier.TrackingUrlTemplate, carrier.IsActive,
        carrier.CreatedByUserId, carrier.CreationDate, carrier.ModifiedByUserId, carrier.ModificationDate,
        VersionTokenCodec.Encode(carrier.RowVersion));

    #endregion
}
