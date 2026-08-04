using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.Operations.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class CustomerProvider : ICustomerProvider
{
    #region Fields

    private readonly MasterDataMutationCoordinator _mutations;
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICustomerViewRepository _views;

    #endregion

    #region Constructors

    public CustomerProvider(ICustomerRepository repository, ICustomerViewRepository views, IUnitOfWork unitOfWork,
        MasterDataMutationCoordinator mutations)
    {
        _repository = repository;
        _views = views;
        _unitOfWork = unitOfWork;
        _mutations = mutations;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<CustomerView>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken)
    {
        request.Validate("id", "name");
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<CustomerView> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _views.GetByIdAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("customer");
    }

    public async Task<CustomerView> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        int id = await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Customer customer = Customer.Create(request, _mutations.UserId, now);
            if (await _repository.AccountNumberExistsAsync(customer.AccountNumber, null, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("account number");

            await _repository.AddAsync(customer);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            _mutations.AddAudit("customer", customer.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return customer.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CustomerView> UpdateAsync(int id, CustomerUpdateRequest request, string expectedVersion,
        CancellationToken cancellationToken)
    {
        VersionTokenCodec.Decode(expectedVersion);
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Customer customer = await _repository.GetForUpdateAsync(id, cancellationToken) ??
                                throw MasterDataMutationCoordinator.NotFound("customer");
            MasterDataMutationCoordinator.EnsureVersion(customer.RowVersion, expectedVersion);
            if (await _repository.AccountNumberExistsAsync(request.AccountNumber?.Trim() ?? string.Empty, id, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("account number");

            bool wasActive = customer.IsActive;
            customer.Replace(request, _mutations.UserId, now);
            await _repository.UpdateAsync(customer);
            _mutations.AddAudit("customer", customer.Id,
                MasterDataMutationCoordinator.GetUpdateAction(wasActive, customer.IsActive),
                "{\"changedFields\":[\"masterData\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return customer.Id;
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
            Customer customer = await _repository.GetForUpdateAsync(id, cancellationToken) ??
                                throw MasterDataMutationCoordinator.NotFound("customer");
            MasterDataMutationCoordinator.EnsureVersion(customer.RowVersion, expectedVersion);
            if (await _repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The customer is referenced and cannot be deleted; deactivate it instead.");

            await _repository.DeleteAsync(customer);
            _mutations.AddAudit("customer", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    #endregion
}
