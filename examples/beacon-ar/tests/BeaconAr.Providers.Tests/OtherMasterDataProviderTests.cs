using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Operations.Repositories;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Providers.MasterData;

namespace BeaconAr.Providers.Tests;

[TestClass]
public sealed class OtherMasterDataProviderTests
{
    #region Public Methods

    [TestMethod]
    public async Task CustomerRejectsInvalidPersistenceShapeBeforeUsingCollaborators()
    {
        var provider = new CustomerProvider(null!, null!, null!, null!, null!, null!, null!, null!);

        MasterDataValidationException exception = await Assert.ThrowsAsync<MasterDataValidationException>(() =>
            provider.CreateAsync(new CustomerCreateRequest(
                "ACCOUNT", "Customer", "customer@example.test", null, 1.235m, 30), CancellationToken.None));

        Assert.IsTrue(exception.Errors.ContainsKey("creditLimit"));
    }

    [TestMethod]
    public async Task AddressRejectsInvalidDefaultBeforeUsingCollaborators()
    {
        var provider = new AddressProvider(null!, null!, null!, null!, null!, null!, null!, null!, null!);

        MasterDataValidationException exception = await Assert.ThrowsAsync<MasterDataValidationException>(() =>
            provider.CreateAsync(new AddressCreateRequest(
                1, "billing", "Address", "Line 1", null, "City", null, "1000", "AR", false, true),
                CancellationToken.None));

        Assert.IsTrue(exception.Errors.ContainsKey("defaultShipping"));
    }

    [TestMethod]
    public async Task CarrierRejectsUnsafeTrackingTemplateBeforeUsingCollaborators()
    {
        var provider = new CarrierProvider(null!, null!, null!, null!, null!, null!, null!, null!);

        MasterDataValidationException exception = await Assert.ThrowsAsync<MasterDataValidationException>(() =>
            provider.CreateAsync(new CarrierCreateRequest(
                "CODE", "Carrier", "Ground", "http://carrier.test/{trackingNumber}"), CancellationToken.None));

        Assert.IsTrue(exception.Errors.ContainsKey("trackingUrlTemplate"));
    }

    #endregion
}
