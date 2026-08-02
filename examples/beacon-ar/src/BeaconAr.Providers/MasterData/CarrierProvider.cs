using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Validation;
using BeaconAr.Domain.Receivables.Entities;
using BeaconAr.Interfaces.Receivables.Entities;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Providers;
using VersionTokenCodec = BeaconAr.Domain.MasterData.Application.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class CarrierProvider
    : EditProviderBase<ICarrier, Carrier, CarrierView, ICarrierRepository, ICarrierViewRepository, int>, ICarrierProvider
{
    #region Fields

    private readonly MasterDataMutationCoordinator _mutations;

    #endregion

    #region Constructors

    public CarrierProvider(IServiceProvider serviceProvider, MasterDataMutationCoordinator mutations)
        : base(serviceProvider)
    {
        _mutations = mutations;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<CarrierDto>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken)
    {
        MasterDataRequestValidator.ValidateSearch(request, "id", "name");
        return SearchLegacyAsync(request, cancellationToken);
    }

    public async Task<CarrierDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ToDto(await ViewRepository.GetByIdAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("carrier"));
    }

    public async Task<CarrierDto> CreateAsync(CarrierCreateRequest request, CancellationToken cancellationToken)
    {
        CarrierCreateRequest value = MasterDataRequestValidator.Normalize(request);
        int id = await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            if (await Repository.CodeExistsAsync(value.Code!, null, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("code");

            Carrier carrier = Carrier.Create(value, _mutations.UserId, now);
            await Repository.AddAsync(carrier);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            _mutations.AddAudit("carrier", carrier.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return carrier.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CarrierDto> UpdateAsync(int id, CarrierUpdateRequest request, string expectedVersion,
        CancellationToken cancellationToken)
    {
        CarrierUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        VersionTokenCodec.Decode(expectedVersion);
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Carrier carrier = await Repository.GetForUpdateAsync(id, cancellationToken) ??
                              throw MasterDataMutationCoordinator.NotFound("carrier");
            MasterDataMutationCoordinator.EnsureVersion(carrier.RowVersion, expectedVersion);
            if (await Repository.CodeExistsAsync(value.Code!, id, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("code");

            bool wasActive = carrier.IsActive;
            carrier.Replace(value, _mutations.UserId, now);
            await Repository.UpdateAsync(carrier);
            _mutations.AddAudit("carrier", carrier.Id,
                MasterDataMutationCoordinator.GetUpdateAction(wasActive, carrier.IsActive),
                "{\"changedFields\":[\"masterData\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return carrier.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        VersionTokenCodec.Decode(expectedVersion);
        return ExecuteDeleteAsync(id, expectedVersion, cancellationToken);
    }

    #endregion

    #region Overrides

    public override Task<PaginatedResultDto<CarrierView>> SearchAsync<TParameters>(TParameters parameters)
    {
        if (parameters is not MasterDataViewSearchParameters search)
            throw new ArgumentException($"{nameof(CarrierProvider)} requires {nameof(MasterDataViewSearchParameters)}.", nameof(parameters));
        MasterDataRequestValidator.ValidateSearch(search, "id", "name");
        return base.SearchAsync(parameters);
    }

    public override Task<CarrierView> AddAsync(CarrierView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CarrierView>> AddAsync(List<CarrierView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<CarrierView> UpdateAsync(CarrierView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CarrierView>> UpdateAsync(List<CarrierView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<CarrierView> SaveAsync(CarrierView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CarrierView>> SaveAsync(IEnumerable<CarrierView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task DeleteAsync(int id) => throw OfficialMasterDataMutationGuard.Create();

    public override Task DeleteAsync(IEnumerable<int> ids) => throw OfficialMasterDataMutationGuard.Create();

    #endregion

    #region Private Methods

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Carrier carrier = await Repository.GetForUpdateAsync(id, cancellationToken) ??
                              throw MasterDataMutationCoordinator.NotFound("carrier");
            MasterDataMutationCoordinator.EnsureVersion(carrier.RowVersion, expectedVersion);
            if (await Repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The carrier is referenced and cannot be deleted; deactivate it instead.");

            await Repository.DeleteAsync(carrier);
            _mutations.AddAudit("carrier", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    private async Task<PageResult<CarrierDto>> SearchLegacyAsync(CarrierSearchRequest request,
        CancellationToken cancellationToken)
    {
        PageResult<CarrierView> result = await ViewRepository.SearchAsync(request, cancellationToken);
        return new PageResult<CarrierDto>(result.Items.Select(ToDto).ToArray(), result.PageNumber, result.PageSize,
            result.TotalPages, result.ItemsCount);
    }

    private static CarrierDto ToDto(CarrierView carrier) => new(
        carrier.Id, carrier.Code, carrier.Name, carrier.ServiceLevel, carrier.TrackingUrlTemplate, carrier.IsActive,
        carrier.CreatedByUserId, carrier.CreationDate, carrier.ModifiedByUserId, carrier.ModificationDate,
        VersionTokenCodec.Encode(carrier.RowVersion));

    #endregion
}
