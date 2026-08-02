using BeaconAr.Domain.Receivables.Entities;

namespace BeaconAr.Domain.Tests;

[TestClass]
public sealed class GeneratedMapperTests
{
    #region Nested Types

    private sealed class MappingServiceProvider : IServiceProvider
    {
        #region Fields

        private readonly ProductMapper _productMapper = new();
        private readonly ProductView _productView = new();
        private readonly QuoteMapper _quoteMapper = new();
        private readonly QuoteView _quoteView = new();

        #endregion

        #region Public Methods

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ProductMapper))
                return _productMapper;
            if (serviceType == typeof(ProductView))
                return _productView;
            if (serviceType == typeof(QuoteMapper))
                return _quoteMapper;
            if (serviceType == typeof(QuoteView))
                return _quoteView;
            return null;
        }

        #endregion
    }

    #endregion

    #region Public Methods

    [TestMethod]
    public void InterfaceMappingProtectsServerOwnedValuesAndCarriesConcurrencyState()
    {
        var serviceProvider = new MappingServiceProvider();
        DateTimeOffset creationDate = new(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        DateTimeOffset modificationDate = creationDate.AddDays(1);
        Product entity = new()
        {
            Id = 17,
            Name = "Original name",
            CreatedByUserId = 11,
            CreationDate = creationDate,
            ModifiedByUserId = 12,
            ModificationDate = modificationDate,
            RowVersion = [1, 1, 1],
        };
        ProductView incoming = new()
        {
            Id = 99,
            Name = "Updated name",
            CreatedByUserId = 91,
            CreationDate = creationDate.AddYears(1),
            ModifiedByUserId = 92,
            ModificationDate = modificationDate.AddYears(1),
            RowVersion = [2, 2, 2],
        };

        Product mapped = entity.MapFrom(serviceProvider, incoming);

        Assert.AreSame(entity, mapped);
        Assert.AreEqual(17, entity.Id);
        Assert.AreEqual("Updated name", entity.Name);
        Assert.AreEqual(11, entity.CreatedByUserId);
        Assert.AreEqual(creationDate, entity.CreationDate);
        Assert.AreEqual(12, entity.ModifiedByUserId);
        Assert.AreEqual(modificationDate, entity.ModificationDate);
        CollectionAssert.AreEqual(new byte[] { 2, 2, 2 }, entity.RowVersion);

        ProductView outgoing = entity.MapTo(serviceProvider);
        Assert.AreEqual(17, outgoing.Id);
        Assert.AreEqual(11, outgoing.CreatedByUserId);
        Assert.AreEqual(creationDate, outgoing.CreationDate);
        Assert.AreEqual(12, outgoing.ModifiedByUserId);
        Assert.AreEqual(modificationDate, outgoing.ModificationDate);
        CollectionAssert.AreEqual(entity.RowVersion, outgoing.RowVersion);
    }

    [TestMethod]
    public void InterfaceMappingProtectsDeletionAndHistoricalSnapshots()
    {
        var serviceProvider = new MappingServiceProvider();
        DateTimeOffset deletionDate = new(2025, 2, 3, 4, 5, 6, TimeSpan.Zero);
        Quote entity = new()
        {
            CustomerId = 10,
            CustomerNameSnapshot = "Historical customer",
            ShippingCitySnapshot = "Historical city",
            DeletionDate = deletionDate,
            DeletedByUserId = 23,
            RowVersion = [3, 3, 3],
        };
        QuoteView incoming = new()
        {
            CustomerId = 20,
            CustomerNameSnapshot = "Injected customer",
            ShippingCitySnapshot = "Injected city",
            DeletionDate = deletionDate.AddYears(1),
            DeletedByUserId = 99,
            RowVersion = [4, 4, 4],
        };

        entity.MapFrom(serviceProvider, incoming);

        Assert.AreEqual(20, entity.CustomerId);
        Assert.AreEqual("Historical customer", entity.CustomerNameSnapshot);
        Assert.AreEqual("Historical city", entity.ShippingCitySnapshot);
        Assert.AreEqual(deletionDate, entity.DeletionDate);
        Assert.AreEqual(23, entity.DeletedByUserId);
        CollectionAssert.AreEqual(new byte[] { 4, 4, 4 }, entity.RowVersion);
    }

    #endregion
}
