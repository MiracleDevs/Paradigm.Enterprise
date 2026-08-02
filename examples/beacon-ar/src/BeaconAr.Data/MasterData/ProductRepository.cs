using BeaconAr.Data.Receivables.Context;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.Receivables.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData;

public sealed class ProductRepository : RepositoryBase<ReceivablesDbContext, int>, IProductRepository
{
    #region Constructors

    public ProductRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, int? excludedId, CancellationToken cancellationToken) =>
        EntityContext.Products.AsNoTracking().AnyAsync(
            product => product.Sku == sku && (!excludedId.HasValue || product.Id != excludedId.Value), cancellationToken);

    public async Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken) =>
        await EntityContext.QuoteLines.AsNoTracking().AnyAsync(line => line.ProductId == id, cancellationToken) ||
        await EntityContext.SalesOrderLines.AsNoTracking().AnyAsync(line => line.ProductId == id, cancellationToken);

    public void Add(Product product) => EntityContext.Products.Add(product);

    public void Delete(Product product) => EntityContext.Products.Remove(product);

    #endregion
}
