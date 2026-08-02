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

public sealed class AddressProvider
    : EditProviderBase<ICustomerAddress, CustomerAddress, CustomerAddressView, IAddressRepository, IAddressViewRepository, int>, IAddressProvider
{
    #region Fields

    private static readonly IReadOnlyList<string> TypeUnavailableMessages =
        Array.AsReadOnly(new[] { "The address type is not available." });

    private readonly ICustomerRepository _customers;
    private readonly MasterDataMutationCoordinator _mutations;

    #endregion

    #region Constructors

    public AddressProvider(
        IServiceProvider serviceProvider,
        ICustomerRepository customers,
        MasterDataMutationCoordinator mutations)
        : base(serviceProvider)
    {
        _customers = customers;
        _mutations = mutations;
    }

    #endregion

    #region Public Methods

    public async Task<PageResult<AddressDto>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken)
    {
        PageResult<CustomerAddressView> result = await SearchForApiAsync(request, cancellationToken);
        return new PageResult<AddressDto>(result.Items.Select(ToDto).ToArray(), result.PageNumber, result.PageSize,
            result.TotalPages, result.ItemsCount);
    }

    public async Task<AddressDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        ToDto(await GetForApiAsync(id, cancellationToken));

    public async Task<AddressDto> CreateAsync(AddressCreateRequest request, CancellationToken cancellationToken) =>
        ToDto(await CreateForApiAsync(request, cancellationToken));

    public async Task<AddressDto> UpdateAsync(int id, AddressUpdateRequest request, string expectedVersion,
        CancellationToken cancellationToken) =>
        ToDto(await UpdateForApiAsync(id, request, expectedVersion, cancellationToken));

    public Task<PageResult<CustomerAddressView>> SearchForApiAsync(AddressSearchRequest request, CancellationToken cancellationToken)
    {
        MasterDataRequestValidator.ValidateSearch(request, "id", "name");
        return ViewRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<CustomerAddressView> GetForApiAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ViewRepository.GetByIdAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("address");
    }

    public async Task<CustomerAddressView> CreateForApiAsync(AddressCreateRequest request, CancellationToken cancellationToken)
    {
        AddressCreateRequest value = MasterDataRequestValidator.Normalize(request);
        int id = await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            await Repository.LockCustomersAsync(new[] { value.CustomerId }, cancellationToken);
            int typeId = await EnsureCustomerAndTypeAsync(value.CustomerId, value.Type!, cancellationToken);
            IReadOnlyList<CustomerAddress> defaults = await Repository.GetDefaultsForUpdateAsync(new[] { value.CustomerId }, cancellationToken);
            List<(CustomerAddress Address, bool Billing, bool Shipping)> cleared = ClearRequestedDefaults(
                defaults, 0, value.DefaultBilling, value.DefaultShipping, now: now);

            CustomerAddress address = CustomerAddress.Create(value, typeId, _mutations.UserId, now);
            await Repository.AddAsync(address);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            AuditClearedDefaults(cleared, now);
            _mutations.AddAudit("address", address.Id, "created", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return address.Id;
        }, "referenced_address");
        return await GetForApiAsync(id, cancellationToken);
    }

    public async Task<CustomerAddressView> UpdateForApiAsync(int id, AddressUpdateRequest request, string expectedVersion, CancellationToken cancellationToken)
    {
        AddressUpdateRequest value = MasterDataRequestValidator.Normalize(request);
        VersionTokenCodec.Decode(expectedVersion);
        await _mutations.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _mutations.UtcNow;
            CustomerAddress address = await Repository.GetForUpdateAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("address");
            MasterDataMutationCoordinator.EnsureVersion(address.RowVersion, expectedVersion);
            int oldCustomerId = address.CustomerId;
            if (oldCustomerId != value.CustomerId && await Repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_address", "A referenced address cannot be moved to another customer.");

            await Repository.LockCustomersAsync(new[] { oldCustomerId, value.CustomerId }, cancellationToken);
            int typeId = await EnsureCustomerAndTypeAsync(value.CustomerId, value.Type!, cancellationToken);
            IReadOnlyList<CustomerAddress> defaults = await Repository.GetDefaultsForUpdateAsync(
                new[] { oldCustomerId, value.CustomerId }, cancellationToken);
            List<(CustomerAddress Address, bool Billing, bool Shipping)> cleared = ClearRequestedDefaults(
                defaults, id, value.DefaultBilling, value.DefaultShipping, value.CustomerId, now);
            if (cleared.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UnitOfWork.CommitChangesAsync();
            }
            if (oldCustomerId != value.CustomerId)
                await Repository.ReparentAsync(address, value, typeId, _mutations.UserId, now, cancellationToken);
            else
            {
                address.Replace(value, typeId, _mutations.UserId, now);
                await Repository.UpdateAsync(address);
            }
            AuditClearedDefaults(cleared, now);
            _mutations.AddAudit("address", address.Id, "updated", "{\"changedFields\":[\"masterData\",\"defaults\"]}", now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return address.Id;
        }, "referenced_address");
        return await GetForApiAsync(id, cancellationToken);
    }

    public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        VersionTokenCodec.Decode(expectedVersion);
        return ExecuteDeleteAsync(id, expectedVersion, cancellationToken);
    }

    #endregion

    #region Overrides

    public override Task<PaginatedResultDto<CustomerAddressView>> SearchAsync<TParameters>(TParameters parameters)
    {
        if (parameters is not MasterDataViewSearchParameters search)
            throw new ArgumentException($"{nameof(AddressProvider)} requires {nameof(MasterDataViewSearchParameters)}.", nameof(parameters));
        MasterDataRequestValidator.ValidateSearch(search, "id", "name");
        return base.SearchAsync(parameters);
    }

    public override Task<CustomerAddressView> AddAsync(CustomerAddressView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CustomerAddressView>> AddAsync(List<CustomerAddressView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<CustomerAddressView> UpdateAsync(CustomerAddressView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CustomerAddressView>> UpdateAsync(List<CustomerAddressView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<CustomerAddressView> SaveAsync(CustomerAddressView view) => throw OfficialMasterDataMutationGuard.Create();

    public override Task<IEnumerable<CustomerAddressView>> SaveAsync(IEnumerable<CustomerAddressView> views) => throw OfficialMasterDataMutationGuard.Create();

    public override Task DeleteAsync(int id) => throw OfficialMasterDataMutationGuard.Create();

    public override Task DeleteAsync(IEnumerable<int> ids) => throw OfficialMasterDataMutationGuard.Create();

    #endregion

    #region Private Methods

    private async Task<int> EnsureCustomerAndTypeAsync(int customerId, string type, CancellationToken cancellationToken)
    {
        if (!await _customers.ExistsAsync(customerId, cancellationToken))
            throw new MasterDataException("not_found", "The customer was not found.");
        int? typeId = await Repository.GetActiveTypeIdAsync(type, cancellationToken);
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
            CustomerAddress address = await Repository.GetForUpdateAsync(id, cancellationToken) ?? throw MasterDataMutationCoordinator.NotFound("address");
            MasterDataMutationCoordinator.EnsureVersion(address.RowVersion, expectedVersion);
            if (await Repository.HasReferencesAsync(id, cancellationToken))
                throw new MasterDataException("referenced_address", "The address is referenced and cannot be deleted.");

            await Repository.DeleteAsync(address);
            _mutations.AddAudit("address", id, "deleted", recordedAt: now);
            cancellationToken.ThrowIfCancellationRequested();
            await UnitOfWork.CommitChangesAsync();
            return id;
        }, "referenced_address");
    }

    private static AddressDto ToDto(CustomerAddressView address) => new(
        address.Id, address.CustomerId, address.AddressTypeCode, address.Label, address.Line1, address.Line2,
        address.City, address.State, address.PostalCode, address.Country.Trim(), address.IsDefaultBilling,
        address.IsDefaultShipping, address.CreatedByUserId, address.CreationDate, address.ModifiedByUserId,
        address.ModificationDate, VersionTokenCodec.Encode(address.RowVersion));

    #endregion
}
