using BeaconAr.Data.MasterData.Context;
using BeaconAr.Data.MasterData.StoredProcedures;
using BeaconAr.Domain.MasterData.Repositories;
using BeaconAr.Domain.MasterData.Entities;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.MasterData.Repositories;

public sealed class ProductRepository : EditRepositoryBase<Product, MasterDataDbContext, int>, IProductRepository
{
    #region Fields

    private static readonly HasProductReferencesProcedure HasReferencesProcedure = new();

    #endregion

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

    public async Task<bool> HasReferencesAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool result = await HasReferencesProcedure.ExecuteAsync(GetDbConnection(), new HasProductReferencesParameters { Id = id }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    #endregion
}
