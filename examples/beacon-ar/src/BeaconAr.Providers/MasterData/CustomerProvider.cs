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

public sealed class CustomerProvider
    : EditProviderBase<ICustomer, Customer, CustomerView, ICustomerRepository, ICustomerViewRepository, int>, ICustomerProvider
{
    #region Fields

    private readonly MasterDataMutationCoordinator _mutations;

    #endregion

    #region Constructors

    public CustomerProvider(IServiceProvider serviceProvider, MasterDataMutationCoordinator mutations)
        : base(serviceProvider)
    {
        _mutations = mutations;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<CustomerView>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken)
    {
        MasterDataRequestValidator.ValidateSearch(request, "id", "name");
        return ViewRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<CustomerView> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ViewRepository.GetByIdAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("customer");
    }

    public async Task<CustomerView> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        CustomerCreateRequest value = MasterDataRequestValidator.Normalize(request);
        int id = await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            if (await Repository.AccountNumberExistsAsync(value.AccountNumber!, null, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("account number");

            Customer customer = Customer.Create(value, _mutations.UserId, now);
            await Repository.AddAsync(customer);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            _mutations.AddAudit("customer", customer.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return customer.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CustomerView> UpdateAsync(int id, CustomerUpdateRequest request, string expectedVersion,
        CancellationToken cancellationToken)
    {
        CustomerUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        VersionTokenCodec.Decode(expectedVersion);
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Customer customer = await Repository.GetForUpdateAsync(id, cancellationToken) ??
                                throw MasterDataMutationCoordinator.NotFound("customer");
            MasterDataMutationCoordinator.EnsureVersion(customer.RowVersion, expectedVersion);
            if (await Repository.AccountNumberExistsAsync(value.AccountNumber!, id, cancellationToken))
                throw MasterDataMutationCoordinator.Duplicate("account number");

            bool wasActive = customer.IsActive;
            customer.Replace(value, _mutations.UserId, now);
            await Repository.UpdateAsync(customer);
            _mutations.AddAudit("customer", customer.Id,
                MasterDataMutationCoordinator.GetUpdateAction(wasActive, customer.IsActive),
                "{\"changedFields\":[\"masterData\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
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

    #region Overrides

    public override Task<PaginatedResultDto<CustomerView>> SearchAsync<TParameters>(TParameters parameters)
    {
        if (parameters is not MasterDataViewSearchParameters search)
            throw new ArgumentException($"{nameof(CustomerProvider)} requires {nameof(MasterDataViewSearchParameters)}.", nameof(parameters));
        MasterDataRequestValidator.ValidateSearch(search, "id", "name");
        return base.SearchAsync(parameters);
    }

    public override Task<CustomerView> AddAsync(CustomerView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CustomerView>> AddAsync(List<CustomerView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<CustomerView> UpdateAsync(CustomerView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CustomerView>> UpdateAsync(List<CustomerView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<CustomerView> SaveAsync(CustomerView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CustomerView>> SaveAsync(IEnumerable<CustomerView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task DeleteAsync(int id) => throw OfficialMasterDataMutationGuard.Create();

    public override Task DeleteAsync(IEnumerable<int> ids) => throw OfficialMasterDataMutationGuard.Create();

    #endregion

    #region Private Methods

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            Customer customer = await Repository.GetForUpdateAsync(id, cancellationToken) ??
                                throw MasterDataMutationCoordinator.NotFound("customer");
            MasterDataMutationCoordinator.EnsureVersion(customer.RowVersion, expectedVersion);
            if (await Repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The customer is referenced and cannot be deleted; deactivate it instead.");

            await Repository.DeleteAsync(customer);
            _mutations.AddAudit("customer", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    #endregion
}
