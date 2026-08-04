using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.Operations.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class CarrierProvider : ICarrierProvider
{
    #region Fields

    private readonly MasterDataMutationCoordinator _mutations;
    private readonly ICarrierRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICarrierViewRepository _views;

    #endregion

    #region Constructors

    public CarrierProvider(ICarrierRepository repository, ICarrierViewRepository views, IUnitOfWork unitOfWork,
        MasterDataMutationCoordinator mutations)
    {
        _repository = repository;
        _views = views;
        _unitOfWork = unitOfWork;
        _mutations = mutations;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<CarrierView>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken)
    {
        request.Validate("id", "name");
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<CarrierView> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _views.GetByIdAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("carrier");
    }

    public async Task<CarrierView> CreateAsync(CarrierCreateRequest request, CancellationToken cancellationToken)
    {
        int id = await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Carrier carrier = Carrier.Create(request, _mutations.UserId, now);
            if (await _repository.CodeExistsAsync(carrier.Code, null, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("code");

            await _repository.AddAsync(carrier);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            _mutations.AddAudit("carrier", carrier.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return carrier.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CarrierView> UpdateAsync(int id, CarrierUpdateRequest request, string expectedVersion,
        CancellationToken cancellationToken)
    {
        VersionTokenCodec.Decode(expectedVersion);
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Carrier carrier = await _repository.GetForUpdateAsync(id, cancellationToken) ??
                              throw MasterDataMutationCoordinator.NotFound("carrier");
            MasterDataMutationCoordinator.EnsureVersion(carrier.RowVersion, expectedVersion);
            if (await _repository.CodeExistsAsync(request.Code?.Trim() ?? string.Empty, id, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("code");

            bool wasActive = carrier.IsActive;
            carrier.Replace(request, _mutations.UserId, now);
            await _repository.UpdateAsync(carrier);
            _mutations.AddAudit("carrier", carrier.Id,
                MasterDataMutationCoordinator.GetUpdateAction(wasActive, carrier.IsActive),
                "{\"changedFields\":[\"masterData\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
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
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Carrier carrier = await _repository.GetForUpdateAsync(id, cancellationToken) ??
                              throw MasterDataMutationCoordinator.NotFound("carrier");
            MasterDataMutationCoordinator.EnsureVersion(carrier.RowVersion, expectedVersion);
            if (await _repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The carrier is referenced and cannot be deleted; deactivate it instead.");

            await _repository.DeleteAsync(carrier);
            _mutations.AddAudit("carrier", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    #endregion
}
