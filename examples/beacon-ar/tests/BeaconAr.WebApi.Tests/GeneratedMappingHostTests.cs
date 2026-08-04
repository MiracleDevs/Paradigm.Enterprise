using BeaconAr.Domain.Access.Entities;
using BeaconAr.Domain.MasterData.Entities;
using BeaconAr.Domain.Operations.Entities;
using BeaconAr.Domain.Sales.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace BeaconAr.WebApi.Tests;

[TestClass]
public sealed class GeneratedMappingHostTests
{
    #region Public Methods

    [TestMethod]
    public void ProgramRegistersGeneratedEntitiesViewsAndMappers()
    {
        using var factory = new BeaconArSecurityTestHost();
        using IServiceScope scope = factory.Services.CreateScope();
        Product entity = scope.ServiceProvider.GetRequiredService<Product>();
        ProductView incoming = scope.ServiceProvider.GetRequiredService<ProductView>();
        ProductMapper mapper = scope.ServiceProvider.GetRequiredService<ProductMapper>();
        incoming.Name = "Mapped through Program";
        incoming.Sku = "HOST-1";
        incoming.Category = "Host test";
        incoming.RowVersion = [1, 2, 3];

        Product mapped = entity.MapFrom(scope.ServiceProvider, incoming);
        ProductView outgoing = mapped.MapTo(scope.ServiceProvider);

        Assert.AreSame(entity, mapped);
        Assert.IsNotNull(mapper);
        Assert.AreEqual("Mapped through Program", outgoing.Name);
    }

    #endregion
}
