using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Repositories;
using Paradigm.Enterprise.Domain.Uow;
using OrderState = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;

namespace BeaconAr.Providers.Sales;

public sealed class SalesOrderProvider : ISalesOrderProvider
{
    #region Fields

    private readonly ISalesOrderRepository _orders;
    private readonly ISalesOrderViewRepository _views;
    private readonly ISalesReferenceRepository _references;
    private readonly SalesWorkflowCoordinator _workflow;

    #endregion

    #region Constructors

    public SalesOrderProvider(
        ISalesOrderRepository orders,
        ISalesOrderViewRepository views,
        ISalesReferenceRepository references,
        SalesWorkflowCoordinator workflow)
    {
        _orders = orders;
        _views = views;
        _references = references;
        _workflow = workflow;
    }

    #endregion

    #region Public Methods

    public Task<PageResult<SalesOrderView>> SearchAsync(SalesOrderSearchRequest request, CancellationToken cancellationToken)
    {
        request.Validate();
        return _views.SearchAsync(request, cancellationToken);
    }

    public async Task<SalesOrderDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _views.GetByIdAsync(id, cancellationToken) ?? throw SalesWorkflowCoordinator.NotFound("sales order");

    public async Task<SalesOrderDto> CreateDirectAsync(SalesOrderCreateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        int id = await _workflow.ExecuteAsync(async () =>
        {
            DateTimeOffset now = _workflow.UtcNow;
            var (customer, address, products, carrier) = await ResolveReferencesAsync(
                request.CustomerId, request.ShippingAddressId, request.Lines, request.CarrierId, cancellationToken);
            string number = await _orders.AllocateNumberAsync(cancellationToken);
            SalesOrder order = SalesOrder.CreateDirectDraft(number, request, customer, address, products,
                carrier, _workflow.UserId, now);
            _orders.Add(order);
            _orders.AddHistory(SalesOrderStatusHistory.Create(order, _workflow.UserId, now));
            cancellationToken.ThrowIfCancellationRequested();
            await _workflow.UnitOfWork.CommitChangesAsync();
            cancellationToken.ThrowIfCancellationRequested();
            _workflow.AddAudit("salesOrder", order.Id, "created", now);
            await _workflow.UnitOfWork.CommitChangesAsync();
            return order.Id;
        });
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SalesOrderDto> UpdateAsync(
        int id,
        SalesOrderUpdateRequest request,
        string expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = VersionTokenCodec.Decode(expectedVersion);
        await _workflow.ExecuteAsync(async () =>
        {
            SalesOrder order = await _orders.GetForUpdateAsync(id, cancellationToken) ?? throw SalesWorkflowCoordinator.NotFound("sales order");
            SalesWorkflowCoordinator.EnsureVersion(order.RowVersion, expectedVersion);
            var (customer, address, products, carrier) = await ResolveReferencesAsync(
                request.CustomerId, request.ShippingAddressId, request.Lines, request.CarrierId, cancellationToken);
            DateTimeOffset now = _workflow.UtcNow;
            IReadOnlyList<SalesOrderLine> lines = order.Replace(request, customer, address, products, carrier,
                _workflow.UserId, now);
            _orders.ReplaceLines(order, lines);
            _workflow.AddAudit("salesOrder", order.Id, "updated", now, metadataJson: "{\"changedFields\":[\"header\",\"lines\"]}");
            await _workflow.UnitOfWork.CommitChangesAsync();
            return order.Id;
        });
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        _ = VersionTokenCodec.Decode(expectedVersion);
        return DeleteCoreAsync(id, expectedVersion, cancellationToken);
    }

    public async Task<SalesOrderDto> TransitionAsync(
        int id,
        SalesOrderStatusTransitionRequest request,
        string expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = VersionTokenCodec.Decode(expectedVersion);
        await _workflow.ExecuteAsync(async () =>
        {
            SalesOrder order = await _orders.GetForUpdateAsync(id, cancellationToken) ?? throw SalesWorkflowCoordinator.NotFound("sales order");
            SalesWorkflowCoordinator.EnsureVersion(order.RowVersion, expectedVersion);
            int? selectedCarrierId = request.CarrierId ?? order.CarrierId;
            CarrierSalesReference? carrier = selectedCarrierId.HasValue
                ? await _references.GetCarrierAsync(selectedCarrierId.Value, cancellationToken)
                : null;
            DateTimeOffset now = _workflow.UtcNow;
            OrderState previous = order.TransitionTo(request, carrier, _workflow.UserId, now);
            _orders.AddHistory(SalesOrderStatusHistory.Create(order, _workflow.UserId, now));
            _workflow.AddAudit("salesOrder", order.Id, "statusTransition", now, Code(previous), Code(request.Status));
            await _workflow.UnitOfWork.CommitChangesAsync();
            return order.Id;
        });
        return await GetByIdAsync(id, cancellationToken);
    }

    #endregion

    #region Private Methods

    private async Task DeleteCoreAsync(int id, string expectedVersion, CancellationToken cancellationToken)
    {
        await _workflow.ExecuteAsync(async () =>
        {
            SalesOrder order = await _orders.GetForUpdateAsync(id, cancellationToken) ?? throw SalesWorkflowCoordinator.NotFound("sales order");
            SalesWorkflowCoordinator.EnsureVersion(order.RowVersion, expectedVersion);
            DateTimeOffset now = _workflow.UtcNow;
            order.Tombstone(_workflow.UserId, now);
            _workflow.AddAudit("salesOrder", order.Id, "deleted", now);
            await _workflow.UnitOfWork.CommitChangesAsync();
            return order.Id;
        });
    }

    private async Task<(CustomerSalesReference Customer, AddressSalesReference Address,
        IReadOnlyDictionary<int, ProductSalesReference> Products, CarrierSalesReference? Carrier)> ResolveReferencesAsync(
        int customerId,
        int addressId,
        IReadOnlyList<SalesLineRequest>? lines,
        int? carrierId,
        CancellationToken cancellationToken)
    {
        if (customerId <= 0)
            throw SalesWorkflowCoordinator.InvalidReference("customerId", "Customer ID must be positive.");
        if (addressId <= 0)
            throw SalesWorkflowCoordinator.InvalidReference("shippingAddressId", "Shipping address ID must be positive.");
        if (carrierId is <= 0)
            throw SalesWorkflowCoordinator.InvalidReference("carrierId", "Carrier ID must be positive.");
        CustomerSalesReference customer = await _references.GetCustomerAsync(customerId, cancellationToken)
            ?? throw SalesWorkflowCoordinator.InvalidReference("customerId", "The selected customer was not found.");
        AddressSalesReference address = await _references.GetAddressAsync(addressId, cancellationToken)
            ?? throw SalesWorkflowCoordinator.InvalidReference("shippingAddressId", "The selected shipping address was not found.");
        IReadOnlyDictionary<int, ProductSalesReference> products = await _references.GetProductsAsync(
            lines?.Where(line => line.ProductId > 0).Select(line => line.ProductId).Distinct().ToArray() ?? [],
            cancellationToken);
        CarrierSalesReference? carrier = carrierId.HasValue
            ? await _references.GetCarrierAsync(carrierId.Value, cancellationToken)
            : null;
        return (customer, address, products, carrier);
    }

    private static string Code(OrderState status) => status.ToString().ToLowerInvariant();

    #endregion
}
