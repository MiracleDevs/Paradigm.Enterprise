using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Sales.Repositories;

public interface ISalesReferenceRepository : IRepository
{
    Task<CustomerSalesReference?> GetCustomerAsync(int id, CancellationToken cancellationToken);

    Task<AddressSalesReference?> GetAddressAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, ProductSalesReference>> GetProductsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    Task<CarrierSalesReference?> GetCarrierAsync(int id, CancellationToken cancellationToken);
}
