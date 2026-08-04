using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Paradigm.Enterprise.Domain.Uow;
using VersionTokenCodec = BeaconAr.Domain.Operations.VersionTokenCodec;

namespace BeaconAr.Providers.MasterData;

public sealed class AddressProvider : IAddressProvider
{
    #region Fields

    private static readonly IReadOnlyList<string> TypeUnavailableMessages =
        Array.AsReadOnly(new[] { "The address type is not available." });

    private readonly ICustomerRepository _customers;
    private readonly MasterDataMutationCoordinator _mutations;
    private readonly IAddressRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAddressViewRepository _views;

    #endregion

    #region Constructors

    public AddressProvider(
        IAddressRepository repository,
        IAddressViewRepository views,
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        MasterDataMutationCoordinator mutations)
    {
        _repository = repository;
        _views = views;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _mutations = mutations;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<CustomerAddressView>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken)
    {
        request.Validate("id", "name");
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<CustomerAddressView> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _views.GetByIdAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("address");
    }

    public async Task<CustomerAddressView> CreateAsync(AddressCreateRequest request, CancellationToken cancellationToken)
    {
        int id = await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            await _repository.LockCustomersAsync(new[] { request.CustomerId }, cancellationToken);
            int typeId = await EnsureCustomerAndTypeAsync(request.CustomerId, request.Type?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken);
            IReadOnlyList<CustomerAddress> defaults = await _repository.GetDefaultsForUpdateAsync(new[] { request.CustomerId }, cancellationToken);
            List<(CustomerAddress Address, bool Billing, bool Shipping)> cleared = ClearRequestedDefaults(
                defaults, 0, request.DefaultBilling, request.DefaultShipping, now: now);

            CustomerAddress address = CustomerAddress.Create(request, typeId, _mutations.UserId, now);
            await _repository.AddAsync(address);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            AuditClearedDefaults(cleared, now);
            _mutations.AddAudit("address", address.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return address.Id;
        }, "referenced_address");
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CustomerAddressView> UpdateAsync(int id, AddressUpdateRequest request, string expectedVersion, CancellationToken cancellationToken)
    {
        VersionTokenCodec.Decode(expectedVersion);
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            CustomerAddress address = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("address");
            MasterDataMutationCoordinator.EnsureVersion(address.RowVersion, expectedVersion);
            int oldCustomerId = address.CustomerId;
            if (oldCustomerId != request.CustomerId && await _repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_address", "A referenced address cannot be moved to another customer.");

            await _repository.LockCustomersAsync(new[] { oldCustomerId, request.CustomerId }, cancellationToken);
            int typeId = await EnsureCustomerAndTypeAsync(request.CustomerId, request.Type?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken);
            IReadOnlyList<CustomerAddress> defaults = await _repository.GetDefaultsForUpdateAsync(
                new[] { oldCustomerId, request.CustomerId }, cancellationToken);
            List<(CustomerAddress Address, bool Billing, bool Shipping)> cleared = ClearRequestedDefaults(
                defaults, id, request.DefaultBilling, request.DefaultShipping, request.CustomerId, now);
            if (cleared.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _unitOfWork.CommitChangesAsync();
            }
            address.Replace(request, typeId, _mutations.UserId, now);
            await _repository.UpdateAsync(address);
            AuditClearedDefaults(cleared, now);
            _mutations.AddAudit("address", address.Id, "updated", "{\"changedFields\":[\"masterData\",\"defaults\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
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
        int? typeId = await _repository.GetActiveTypeIdAsync(type, cancellationToken);
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
        int userId = _mutations.UserId;
        DateTimeOffset timestamp = now ?? _mutations.UtcNow;
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
                _mutations.AddAudit("address", address.Id, "defaultBillingCleared", "{\"changedFields\":[\"defaultBilling\"]}", recordedAt);
            if (shipping)
                _mutations.AddAudit("address", address.Id, "defaultShippingCleared", "{\"changedFields\":[\"defaultShipping\"]}", recordedAt);
        }
    }

    private async Task ExecuteDeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            CustomerAddress address = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("address");
            MasterDataMutationCoordinator.EnsureVersion(address.RowVersion, expectedVersion);
            if (await _repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_address", "The address is referenced and cannot be deleted.");

            await _repository.DeleteAsync(address);
            _mutations.AddAudit("address", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await _unitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_address");
    }

    #endregion
}
