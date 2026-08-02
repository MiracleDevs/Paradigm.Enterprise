using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Validation;
using BeaconAr.Domain.Receivables.Entities;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.MasterData.Application.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class CarrierProvider : MasterDataProviderBase, ICarrierProvider
{
    #region Fields

    private readonly ICarrierRepository _carriers;
    private readonly ICarrierViewRepository _views;

    #endregion

    #region Constructors

    public CarrierProvider(
        ICarrierRepository carriers,
        ICarrierViewRepository views,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        IMasterDataPersistenceErrorClassifier errorClassifier,
        IPersistenceSession persistenceSession)
        : base(unitOfWork, auditLogs, operationContext, timeProvider, errorClassifier, persistenceSession)
    {
        _carriers = carriers;
        _views = views;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<CarrierDto>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken)
    {
        MasterDataRequestValidator.ValidateSearch(request, "id", "name");
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<CarrierDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdAsync(id, cancellationToken) ?? throw NotFound("carrier");

    public async Task<CarrierDto> CreateAsync(CarrierCreateRequest request, CancellationToken cancellationToken)
    {
        CarrierCreateRequest value = MasterDataRequestValidator.Normalize(request);
        int id = await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            if (await _carriers.CodeExistsAsync(value.Code!, null, cancellationToken))
                throw Duplicate("code");

            Carrier carrier = Carrier.Create(value, OperationContext.UserId, now);
            _carriers.Add(carrier);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            AddAudit("carrier", carrier.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return carrier.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CarrierDto> UpdateAsync(int id, CarrierUpdateRequest request, string expectedVersion, CancellationToken cancellationToken)
    {
        CarrierUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        VersionTokenCodec.Decode(expectedVersion);
        await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            Carrier carrier = await _carriers.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("carrier");
            EnsureVersion(carrier.RowVersion, expectedVersion);
            if (await _carriers.CodeExistsAsync(value.Code!, id, cancellationToken))
                throw Duplicate("code");

            bool wasActive = carrier.IsActive;
            carrier.Replace(value, OperationContext.UserId, now);
            AddAudit("carrier", carrier.Id, GetUpdateAction(wasActive, carrier.IsActive), "{\"changedFields\":[\"masterData\"]}", now);
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

    #region Private Methods

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            Carrier carrier = await _carriers.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("carrier");
            EnsureVersion(carrier.RowVersion, expectedVersion);
            if (await _carriers.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The carrier is referenced and cannot be deleted; deactivate it instead.");

            _carriers.Delete(carrier);
            AddAudit("carrier", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    #endregion
}
