using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Validation;
using BeaconAr.Domain.Receivables.Generated;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.MasterData.Application.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class AddressProvider : MasterDataProviderBase, IAddressProvider
{
    #region Fields

    private static readonly IReadOnlyList<string> TypeUnavailableMessages =
        Array.AsReadOnly(new[] { "The address type is not available." });

    private readonly IAddressRepository _addresses;
    private readonly IAddressViewRepository _views;
    private readonly ICustomerRepository _customers;

    #endregion

    #region Constructors

    public AddressProvider(
        IAddressRepository addresses,
        IAddressViewRepository views,
        ICustomerRepository customers,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IApplicationOperationContext operationContext,
        TimeProvider timeProvider,
        IMasterDataPersistenceErrorClassifier errorClassifier,
        IPersistenceSession persistenceSession)
        : base(unitOfWork, auditLogs, operationContext, timeProvider, errorClassifier, persistenceSession)
    {
        _addresses = addresses;
        _views = views;
        _customers = customers;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<AddressDto>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken)
    {
        MasterDataRequestValidator.ValidateSearch(request, "id", "name");
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<AddressDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdAsync(id, cancellationToken) ?? throw NotFound("address");

    public async Task<AddressDto> CreateAsync(AddressCreateRequest request, CancellationToken cancellationToken)
    {
        AddressCreateRequest value = MasterDataRequestValidator.Normalize(request);
        int id = await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            await _addresses.LockCustomersAsync(new[] { value.CustomerId }, cancellationToken);
            int typeId = await EnsureCustomerAndTypeAsync(value.CustomerId, value.Type!, cancellationToken);
            IReadOnlyList<CustomerAddress> defaults = await _addresses.GetDefaultsForUpdateAsync(new[] { value.CustomerId }, cancellationToken);
            List<(CustomerAddress Address, bool Billing, bool Shipping)> cleared = ClearRequestedDefaults(
                defaults, 0, value.DefaultBilling, value.DefaultShipping, now: now);

            CustomerAddress address = CustomerAddress.Create(value, typeId, OperationContext.UserId, now);
            _addresses.Add(address);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            AuditClearedDefaults(cleared, now);
            AddAudit("address", address.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return address.Id;
        }, "referenced_address");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<AddressDto> UpdateAsync(int id, AddressUpdateRequest request, string expectedVersion, CancellationToken cancellationToken)
    {
        AddressUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        VersionTokenCodec.Decode(expectedVersion);
        await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            CustomerAddress address = await _addresses.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("address");
            EnsureVersion(address.RowVersion, expectedVersion);
            int oldCustomerId = address.CustomerId;
            if (oldCustomerId != value.CustomerId && await _addresses.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_address", "A referenced address cannot be moved to another customer.");

            await _addresses.LockCustomersAsync(new[] { oldCustomerId, value.CustomerId }, cancellationToken);
            int typeId = await EnsureCustomerAndTypeAsync(value.CustomerId, value.Type!, cancellationToken);
            IReadOnlyList<CustomerAddress> defaults = await _addresses.GetDefaultsForUpdateAsync(
                new[] { oldCustomerId, value.CustomerId }, cancellationToken);
            List<(CustomerAddress Address, bool Billing, bool Shipping)> cleared = ClearRequestedDefaults(
                defaults, id, value.DefaultBilling, value.DefaultShipping, value.CustomerId, now);
            if (cleared.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UnitOfWork.CommitChangesAsync();
            }
            if (oldCustomerId != value.CustomerId)
                await _addresses.ReparentAsync(address, value, typeId, OperationContext.UserId, now, cancellationToken);
            else
                address.Replace(value, typeId, OperationContext.UserId, now);
            AuditClearedDefaults(cleared, now);
            AddAudit("address", address.Id, "updated", "{\"changedFields\":[\"masterData\",\"defaults\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return address.Id;
        }, "referenced_address");
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        VersionTokenCodec.Decode(expectedVersion);
        return ExecuteDeleteAsync(id, expectedVersion, cancellationToken);
    }

    #endregion

    #region Private Methods

    private async Task<int> EnsureCustomerAndTypeAsync(int customerId, string type, CancellationToken cancellationToken)
    {
        if (!await _customers.ExistsAsync(customerId, cancellationToken))
            throw new MasterDataException("not_found", "The customer was not found.");
        int? typeId = await _addresses.GetActiveTypeIdAsync(type, cancellationToken);
        if (!typeId.HasValue)
            throw new MasterDataValidationException(new Dictionary<string, IReadOnlyList<string>>
            {
                ["type"] = TypeUnavailableMessages,
            });

        return typeId.Value;
    }

    private List<(CustomerAddress Address, bool Billing, bool Shipping)> ClearRequestedDefaults(IReadOnlyList<CustomerAddress> defaults, int excludedId,
        bool billing, bool shipping, int? destinationCustomerId = null, DateTimeOffset? now = null)
    {
        var cleared = new List<(CustomerAddress Address, bool Billing, bool Shipping)>();
        int userId = OperationContext.UserId;
        DateTimeOffset timestamp = now ?? TimeProvider.GetUtcNow();
        foreach (CustomerAddress existing in defaults.Where(item => item.Id != excludedId &&
                     (!destinationCustomerId.HasValue || item.CustomerId == destinationCustomerId.Value)))
        {
            bool clearedBilling = billing && existing.IsDefaultBilling;
            bool clearedShipping = shipping && existing.IsDefaultShipping;
            if (clearedBilling)
                existing.ClearBillingDefault(userId, timestamp);
            if (clearedShipping)
                existing.ClearShippingDefault(userId, timestamp);
            if (clearedBilling || clearedShipping)
                cleared.Add((existing, clearedBilling, clearedShipping));
        }

        return cleared;
    }

    private void AuditClearedDefaults(IEnumerable<(CustomerAddress Address, bool Billing, bool Shipping)> cleared,
        DateTimeOffset recordedAt)
    {
        foreach ((CustomerAddress address, bool billing, bool shipping) in cleared)
        {
            if (billing)
                AddAudit("address", address.Id, "defaultBillingCleared", "{\"changedFields\":[\"defaultBilling\"]}", recordedAt);
            if (shipping)
                AddAudit("address", address.Id, "defaultShippingCleared", "{\"changedFields\":[\"defaultShipping\"]}", recordedAt);
        }
    }

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await ExecuteMutationAsync(async () =>
        {
            DateTimeOffset now = TimeProvider.GetUtcNow();
            CustomerAddress address = await _addresses.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound("address");
            EnsureVersion(address.RowVersion, expectedVersion);
            if (await _addresses.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_address", "The address is referenced and cannot be deleted.");

            _addresses.Delete(address);
            AddAudit("address", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_address");
    }

    #endregion
}
