using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Validation;
using BeaconAr.Domain.Receivables.Generated;
using Paradigm.Enterprise.Domain.Uow;

namespace BeaconAr.Providers.MasterData;

public sealed class CustomerProvider : MasterDataProviderBase, ICustomerProvider
{
    #region Fields

    private readonly ICustomerRepository _customers;
    private readonly ICustomerViewRepository _views;

    #endregion

    #region Constructors

    public CustomerProvider(
        ICustomerRepository customers,
        ICustomerViewRepository views,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        IMasterDataPersistenceErrorClassifier errorClassifier,
        IMasterDataPersistenceSession persistenceSession)
        : base(unitOfWork, auditLogs, operationContext, timeProvider, errorClassifier, persistenceSession)
    {
        _customers = customers;
        _views = views;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<CustomerDto>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken)
    {
        MasterDataRequestValidator.ValidateSearch(request, "id", "name");
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<CustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdAsync(id, cancellationToken) ?? throw NotFound("customer");

    public async Task<CustomerDto> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        CustomerCreateRequest value = MasterDataRequestValidator.Normalize(request);
        int id = await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            if (await _customers.AccountNumberExistsAsync(value.AccountNumber!, null, cancellationToken))
                throw Duplicate("account number");

            Customer customer = Customer.Create(value, OperationContext.UserId, now);
            _customers.Add(customer);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            AddAudit("customer", customer.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return customer.Id;
        }, "referenced_record");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CustomerDto> UpdateAsync(int id, CustomerUpdateRequest request, string expectedVersion, CancellationToken cancellationToken)
    {
        CustomerUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        VersionTokenCodec.Decode(expectedVersion);
        await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            Customer customer = await _customers.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("customer");
            EnsureVersion(customer.RowVersion, expectedVersion);
            if (await _customers.AccountNumberExistsAsync(value.AccountNumber!, id, cancellationToken))
                throw Duplicate("account number");

            bool wasActive = customer.IsActive;
            customer.Replace(value, OperationContext.UserId, now);
            AddAudit("customer", customer.Id, GetUpdateAction(wasActive, customer.IsActive), "{\"changedFields\":[\"masterData\"]}", now);
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

    #region Private Methods

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            Customer customer = await _customers.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("customer");
            EnsureVersion(customer.RowVersion, expectedVersion);
            if (await _customers.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_record", "The customer is referenced and cannot be deleted; deactivate it instead.");

            _customers.Delete(customer);
            AddAudit("customer", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_record");
    }

    #endregion
}
